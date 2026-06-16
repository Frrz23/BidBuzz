# BidBuzz / SHAH Project Diagrams

These Mermaid diagrams are prepared for project documentation. They cover the main diagrams usually required in a final defence report: system architecture, use case, ERD, class relationships, data flow, activity flow, and sequence diagrams.

## 1. System Architecture Diagram

```mermaid
flowchart TB
    User[Buyer / Seller / Admin] --> Browser[Web Browser]
    Browser --> MVC[ASP.NET Core MVC App<br/>SHAH Project]

    MVC --> Controllers[Controllers<br/>Home, Item, Bid, Auction, Payment, User]
    MVC --> Views[Razor Views<br/>Bootstrap + JavaScript]
    MVC --> Identity[ASP.NET Core Identity]
    MVC --> SignalR[SignalR BidHub]
    MVC --> Hangfire[Hangfire Background Jobs]

    Controllers --> UnitOfWork[Unit of Work]
    UnitOfWork --> Repositories[Repositories]
    Repositories --> DbContext[ApplicationDbContext]
    DbContext --> SqlServer[(SQL Server Database)]

    Controllers --> Stripe[Stripe Checkout]
    Hangfire --> AuctionScheduler[Auction Scheduler Service]
    AuctionScheduler --> Repositories
    SignalR --> Browser
```

## 2. Use Case Diagram

```mermaid
flowchart LR
    Buyer((Buyer))
    Seller((Seller))
    Admin((Admin))

    Register[Register / Login]
    Browse[Browse Items]
    ViewDetails[View Auction Details]
    PlaceBid[Place Manual Bid]
    SetAutoBid[Set Auto Bid]
    ViewHistory[View Bid History]
    Pay[Pay for Won Item]
    Review[Give Review]

    ListItem[List Item]
    ManageItems[Manage Own Items]
    ViewPayments[View Payment Status]

    ManageUsers[Manage Users]
    ManageCategories[Manage Categories]
    ManageAuctions[Manage Auctions]
    ManageSchedule[Manage Auction Schedule]

    Buyer --> Register
    Buyer --> Browse
    Buyer --> ViewDetails
    Buyer --> PlaceBid
    Buyer --> SetAutoBid
    Buyer --> ViewHistory
    Buyer --> Pay
    Buyer --> Review

    Seller --> Register
    Seller --> ListItem
    Seller --> ManageItems
    Seller --> ViewPayments
    Seller --> Review

    Admin --> ManageUsers
    Admin --> ManageCategories
    Admin --> ManageAuctions
    Admin --> ManageSchedule
```

## 3. Entity Relationship Diagram

```mermaid
erDiagram
    ApplicationUser ||--o{ Item : lists
    ApplicationUser ||--o{ Bid : places
    ApplicationUser ||--o{ AutoBid : sets
    ApplicationUser ||--o{ Notification : receives
    ApplicationUser ||--o{ Review : writes
    ApplicationUser ||--o{ Review : receives

    Category ||--o{ Item : contains
    Item ||--o{ Auction : has
    Item ||--o{ Review : reviewed_for
    Auction ||--o{ Bid : receives
    Auction ||--o{ AutoBid : has

    ApplicationUser {
        string Id
        string Full_Name
        int Age
        string Address
        string Email
    }

    Category {
        int Id
        string Name
    }

    Item {
        int Id
        string Name
        string Description
        decimal StartingPrice
        string ImageUrl
        int CategoryId
        int Quantity
        string UserId
    }

    Auction {
        int Id
        int ItemId
        datetime StartTime
        datetime EndTime
        int Status
        int ExtensionCount
        string WinnerId
        int PaymentStatus
    }

    Bid {
        int Id
        int AuctionId
        decimal Amount
        datetime BidTime
        string UserId
    }

    AutoBid {
        int Id
        int AuctionId
        string UserId
        decimal MaxAmount
        datetime CreatedAt
        bool IsActive
    }

    Notification {
        int Id
        string UserId
        string Message
        bool IsRead
        datetime CreatedAt
    }

    Review {
        int Id
        int Rating
        string Comment
        string ReviewerId
        string ReviewedUserId
        int ItemId
        datetime CreatedAt
    }
```

## 4. Data Flow Diagram - Level 0

```mermaid
flowchart LR
    User[User] -->|Registration, login, bids, item data| System[BidBuzz / SHAH Auction System]
    Admin[Admin] -->|User, category, auction controls| System
    System -->|Pages, notifications, bid updates| User
    System -->|Reports and management views| Admin
    System -->|Payment request| Stripe[Stripe Payment Gateway]
    Stripe -->|Payment result| System
    System -->|Read / write data| DB[(SQL Server Database)]
```

## 5. Data Flow Diagram - Level 1

```mermaid
flowchart TB
    User[Buyer / Seller] --> Auth[Authentication Module]
    User --> ItemModule[Item Management Module]
    User --> BidModule[Bidding Module]
    User --> PaymentModule[Payment Module]
    User --> ReviewModule[Review Module]

    Admin[Admin] --> AdminModule[Admin Management Module]

    Auth --> UserStore[(Users / Roles)]
    ItemModule --> ItemStore[(Items / Categories)]
    BidModule --> AuctionStore[(Auctions / Bids / AutoBids)]
    PaymentModule --> Stripe[Stripe Checkout]
    PaymentModule --> AuctionStore
    ReviewModule --> ReviewStore[(Reviews)]
    AdminModule --> UserStore
    AdminModule --> ItemStore
    AdminModule --> AuctionStore

    BidModule --> SignalR[SignalR Real-time Updates]
    SignalR --> User
    Scheduler[Hangfire Scheduler] --> AuctionStore
```

## 6. Auction Bidding Sequence Diagram

```mermaid
sequenceDiagram
    actor Buyer
    participant Browser
    participant BidController
    participant BidRepository
    participant AutoBidRepository
    participant Database
    participant BidHub

    Buyer->>Browser: Enter bid amount
    Browser->>BidController: Submit PlaceBid request
    BidController->>BidRepository: Validate current auction and minimum bid
    BidRepository->>Database: Save manual bid
    BidController->>AutoBidRepository: Process active auto-bids
    AutoBidRepository->>Database: Read auto-bid rules
    AutoBidRepository->>Database: Save required proxy bid if needed
    BidController->>BidHub: Broadcast bid update
    BidHub-->>Browser: ReceiveBidUpdate event
    Browser-->>Buyer: Show updated highest bid
```

## 7. Auto Bidding Activity Diagram

```mermaid
flowchart TD
    Start([Start]) --> Load[Load active auto-bids for auction]
    Load --> Highest[Find current highest bid]
    Highest --> Increment[Calculate dynamic bid increment]
    Increment --> Required[Calculate minimum required bid]
    Required --> Eligible{Any eligible auto-bid?}
    Eligible -- No --> End([End])
    Eligible -- Yes --> Select[Select highest max auto-bid]
    Select --> Check{Can outbid current highest?}
    Check -- No --> Deactivate[Deactivate exhausted auto-bid]
    Deactivate --> Notify[Create notification]
    Notify --> End
    Check -- Yes --> ProxyBid[Create proxy bid]
    ProxyBid --> Save[Save bid to database]
    Save --> Broadcast[Broadcast real-time update]
    Broadcast --> End
```

## 8. Auction Lifecycle Activity Diagram

```mermaid
flowchart TD
    Draft[Item Listed] --> Scheduled[Auction Scheduled]
    Scheduled --> StartJob[Hangfire Start Auction Job]
    StartJob --> Active[Auction In Progress]
    Active --> BidPlaced{Bid placed?}
    BidPlaced -- Yes --> NearEnd{Within last 5 minutes?}
    NearEnd -- Yes --> Extend{Extension limit not reached?}
    Extend -- Yes --> ExtendTime[Extend auction end time]
    Extend -- No --> Continue[Continue auction]
    NearEnd -- No --> Continue
    BidPlaced -- No --> Continue
    Continue --> EndTime{End time reached?}
    EndTime -- No --> Active
    EndTime -- Yes --> EndJob[Hangfire End Auction Job]
    EndJob --> HasBid{Any bids?}
    HasBid -- Yes --> Sold[Mark as Sold and set winner]
    HasBid -- No --> Relist{Relist count below limit?}
    Relist -- Yes --> Scheduled
    Relist -- No --> Unsold[Mark as Unsold]
    Sold --> Payment[Await Payment]
    Payment --> Completed[Payment Paid / Completed]
```

## 9. Payment Sequence Diagram

```mermaid
sequenceDiagram
    actor Buyer
    participant PaymentPage
    participant PaymentController
    participant Stripe
    participant Database

    Buyer->>PaymentPage: Click Pay Now
    PaymentPage->>PaymentController: Request checkout session
    PaymentController->>Database: Load won auction and item
    PaymentController->>Stripe: Create checkout session
    Stripe-->>PaymentController: Return checkout URL
    PaymentController-->>Buyer: Redirect to Stripe checkout
    Buyer->>Stripe: Complete payment
    Stripe-->>PaymentController: Redirect to success URL
    PaymentController->>Database: Update payment status to Paid
    PaymentController-->>PaymentPage: Show payment success page
```

## 10. Review Sequence Diagram

```mermaid
sequenceDiagram
    actor User
    participant ReviewPage
    participant ReviewController
    participant Database

    User->>ReviewPage: Open review form
    ReviewPage->>ReviewController: Submit rating and comment
    ReviewController->>Database: Check sold item and review eligibility
    Database-->>ReviewController: Return transaction details
    ReviewController->>Database: Save review
    ReviewController-->>ReviewPage: Redirect to reviews / history
```

## 11. Class Relationship Diagram

```mermaid
classDiagram
    class ApplicationUser {
        +string Id
        +string Full_Name
        +int Age
        +string Address
        +string Role
    }

    class Category {
        +int Id
        +string Name
    }

    class Item {
        +int Id
        +string Name
        +string Description
        +decimal StartingPrice
        +string ImageUrl
        +int Quantity
        +decimal CurrentBid
    }

    class Auction {
        +int Id
        +DateTime StartTime
        +DateTime EndTime
        +AuctionStatus Status
        +int ExtensionCount
        +string WinnerId
        +PaymentStatus PaymentStatus
    }

    class Bid {
        +int Id
        +decimal Amount
        +DateTime BidTime
    }

    class AutoBid {
        +int Id
        +decimal MaxAmount
        +DateTime CreatedAt
        +bool IsActive
    }

    class Review {
        +int Id
        +int Rating
        +string Comment
        +DateTime CreatedAt
    }

    class Notification {
        +int Id
        +string Message
        +bool IsRead
        +DateTime CreatedAt
    }

    ApplicationUser "1" --> "many" Item
    ApplicationUser "1" --> "many" Bid
    ApplicationUser "1" --> "many" AutoBid
    ApplicationUser "1" --> "many" Notification
    ApplicationUser "1" --> "many" Review
    Category "1" --> "many" Item
    Item "1" --> "many" Auction
    Item "1" --> "many" Review
    Auction "1" --> "many" Bid
    Auction "1" --> "many" AutoBid
```

