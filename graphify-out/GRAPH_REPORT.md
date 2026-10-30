# Graph Report - KBS  (2026-10-30)

## Corpus Check
- 265 files · ~87,244 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 38 file(s) not represented in the graph (top: .css 33, (none) 2, .props 1)

## Summary
- 1745 nodes · 4432 edges · 105 communities (81 shown, 24 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 341 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- ShopSettings
- AuthService
- OrderResponseDto
- AiModule
- Task
- App.jsx
- SaveShopSettingsDto
- CategoryResponseDto
- CancellationToken
- KBS.Tests.csproj
- UserResponseDto
- microsoft_entityframeworkcore
- package.json
- dateTime()
- Storefront.jsx
- KBS.BLL.Interfaces
- HttpGet
- KBS.DAL.Data
- Fact
- DashboardResponseDto
- BrandResponseDto
- MigrationBuilder
- Order lifecycle
- DateTimeOffset
- useHomeContent()
- react
- src/services/api.js
- KbsDbContext
- Product
- CartResponseDto
- system_componentmodel_dataannotations
- KBS.Models.Entities
- ProductResponseDto
- ai_auto.py
- compilerOptions
- src_backup/smoke-render.mjs
- ChatController
- Brand
- InventoryManagement.jsx
- compilerOptions
- http
- ProductUpdateDto
- StaffResponses.cs
- ProductCreateDto
- Order
- InventoryResponseDto
- .Likes_are_unique_private_and_keep_unavailabl
- InventoryMovement
- User
- ActionResult
- Program.cs
- ProductService
- StockAdjustmentRequest
- StaffSummaryDto
- ShopSettingsService
- StaffInventoryDto
- TestApiFactory
- KBS.Models.DTOs.Orders
- LikelistItemResponseDto
- src/pages/admin/Dashboard.jsx
- .OnModelCreating()
- InventoryMovementDto
- StaffCustomerDto
- ChatMessage
- ProductReview
- Role
- ShopProvider.tsx
- .Reviews_require_completed_purchase_validate_
- CartItemResponseDto
- ChatConversationDto
- CartItem
- ChatConversation
- OrderDetail
- OrderNote
- InventoryService.cs
- Category
- Inventory
- LikelistItem
- ProductImage
- .Settings_are_admin_only_public_fields_are_sa
- scripts
- ShopManagement.jsx
- ApiResponse
- ProductImageDto
- StaffReviewDto
- .oxlintrc.json
- React storefront architecture
- BrandUpdateDto
- dependencies
- StaffOperationFilter
- .Admin_crud_validates_duplicates_protects_sel
- KBS.BLL/Class1.cs
- KBS.DAL/Class1.cs
- KBS.Models/Class1.cs
- vite.config.js
- tsconfig.json
- Category management

## God Nodes (most connected - your core abstractions)
1. `KbsDbContext` - 91 edges
2. `KBS.Models.Entities` - 53 edges
3. `Product` - 49 edges
4. `KBS.DAL.Data` - 47 edges
5. `react` - 45 edges
6. `User` - 43 edges
7. `Order` - 42 edges
8. `ProductResponseDto` - 41 edges
9. `OrderResponseDto` - 37 edges
10. `KBS.BLL.Interfaces` - 35 edges

## Surprising Connections (you probably didn't know these)
- `Historical entity snapshot` --semantically_similar_to--> `PostgreSQL relational schema`  [INFERRED] [semantically similar]
  entities_snapshot.txt → database/README.md
- `Database-backed UI and engineering rules` --conceptually_related_to--> `React storefront architecture`  [INFERRED]
  AGENT.md → frontend/README.md
- `Admin dashboard` --shares_data_with--> `Order lifecycle`  [EXTRACTED]
  database/admin-dashboard-mapping.md → README.md
- `Worker heartbeat and diagnostics` --references--> `AiEvents audit history`  [EXTRACTED]
  ai/README.md → database/ai-management-design.md
- `KBS keyboard commerce` --references--> `PostgreSQL relational schema`  [EXTRACTED]
  README.md → database/README.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Atomic automatic order confirmation** — ai_readme_ai_auto, database_ai_management_design_ai_modules, database_ai_management_design_ai_events, readme_order_lifecycle, database_ai_management_design_serializable_decisions [EXTRACTED 1.00]

## Communities (105 total, 24 thin omitted)

### Community 0 - "ShopSettings"
Cohesion: 0.05
Nodes (40): PaymentsController, PaymentService, BankOptions, AccountName, AccountNumber, BankBin, BankName, PaymentOptions (+32 more)

### Community 1 - "AuthService"
Cohesion: 0.06
Nodes (28): AiWorkerKeyFilter, AdminSeeder, IJwtService, AuthService, JwtService, ChangePasswordDto, ConfirmNewPassword, CurrentPassword (+20 more)

### Community 2 - "OrderResponseDto"
Cohesion: 0.07
Nodes (31): OrderService, CreateOrderDto, CustomerName, OrderItems, PaymentMethod, PhoneNumber, ShippingAddress, OrderDetailResponseDto (+23 more)

### Community 3 - "AiModule"
Cohesion: 0.07
Nodes (33): AiManagementController, AiManagementService, AiDecisionRequest, Decision, AiDecisionResponseDto, AiEnabledRequest, IsEnabled, AiEventResponseDto (+25 more)

### Community 4 - "Task"
Cohesion: 0.11
Nodes (6): AuthController, ShopSettingsController, UsersController, IAuthService, IUserService, UserRoleDto

### Community 5 - "App.jsx"
Cohesion: 0.09
Nodes (29): Admin(), App(), Catalog(), Checkout(), Detail(), Empty(), Keyboard(), Metric() (+21 more)

### Community 6 - "SaveShopSettingsDto"
Cohesion: 0.05
Nodes (34): PagedResponse, Items, Page, PageSize, TotalItems, TotalPages, ProductReviewResponseDto, Comment (+26 more)

### Community 7 - "CategoryResponseDto"
Cohesion: 0.10
Nodes (19): CategoriesController, ICategoryService, CategoryService, CategoryCreateDto, Description, IsActive, Name, CategoryResponseDto (+11 more)

### Community 9 - "KBS.Tests.csproj"
Cohesion: 0.08
Nodes (26): net9.0, Microsoft.EntityFrameworkCore.Design, Npgsql.EntityFrameworkCore.PostgreSQL, net9.0, Microsoft.EntityFrameworkCore, Microsoft.NET.Sdk, net9.0, Microsoft.EntityFrameworkCore (+18 more)

### Community 10 - "UserResponseDto"
Cohesion: 0.11
Nodes (21): UserService, CreateUserDto, ConfirmPassword, Password, SaveUserDto, Email, FullName, IsActive (+13 more)

### Community 11 - "microsoft_entityframeworkcore"
Cohesion: 0.11
Nodes (7): KBS.Models.DTOs.Ai, KBS.BLL.Services, KBS.Models.DTOs.Shop, KBS.Models.DTOs.Payments, KBS.Models.DTOs.Categories, KBS.Models.DTOs.Cart, KBS.Models.DTOs.Staff

### Community 12 - "package.json"
Cohesion: 0.09
Nodes (31): devDependencies, autoprefixer@^10.4.18, eslint@^9.9.1, @eslint/js@^9.9.1, eslint-plugin-react-hooks@^5.1.0-rc.0, eslint-plugin-react-refresh@^0.4.11, globals@^15.9.0, oxlint@^1.81.0 (+23 more)

### Community 13 - "dateTime()"
Cohesion: 0.25
Nodes (22): AIManagement(), connections, kinds, CustomerDetails(), CustomerManager(), Inventory(), InventoryDetails(), OrderDetails() (+14 more)

### Community 14 - "Storefront.jsx"
Cohesion: 0.13
Nodes (22): Footer(), Login(), Register(), Account(), Cart(), money(), Likelist(), money() (+14 more)

### Community 15 - "KBS.BLL.Interfaces"
Cohesion: 0.20
Nodes (4): KBS.Models.DTOs.Brands, KBS.Models.DTOs.Dashboard, KBS.BLL.Interfaces, KBS.API.Controllers

### Community 16 - "HttpGet"
Cohesion: 0.12
Nodes (5): AiWorkerController, BrandsController, ProductReviewsController, ProductsController, IProductService

### Community 18 - "Fact"
Cohesion: 0.14
Nodes (12): AuthResponseDto, AccessToken, User, LoginDto, LoginIdentifier, Password, AuthorizationHttpTests, LikelistHttpTests (+4 more)

### Community 19 - "DashboardResponseDto"
Cohesion: 0.10
Nodes (20): AdminDashboardController, WeatherForecast, Date, Summary, TemperatureC, TemperatureF, IDashboardService, DashboardService (+12 more)

### Community 20 - "BrandResponseDto"
Cohesion: 0.14
Nodes (12): IBrandService, BrandService, BrandCreateDto, Description, IsActive, Name, BrandResponseDto, CreatedAt (+4 more)

### Community 21 - "MigrationBuilder"
Cohesion: 0.12
Nodes (7): SeedRolesAndCategories, AddUniquePhoneNumber, AddProductImages, AddStaffRole, AddCartItems, AddStaffOperations, AddHomeContentAndAvailability

### Community 22 - "Order lifecycle"
Cohesion: 0.12
Nodes (25): AI auto-confirmation worker, Worker heartbeat and diagnostics, Admin account management, Admin dashboard, AiEvents audit history, AI management and Staff read-only access, AiModules configuration, Serializable AI decisions (+17 more)

### Community 23 - "DateTimeOffset"
Cohesion: 0.12
Nodes (6): InitialCreate, AddLikelist, AddProductReviews, AddAiManagement, AddShopSettings, KbsDbContextModelSnapshot

### Community 24 - "useHomeContent()"
Cohesion: 0.17
Nodes (18): BentoGrid(), features, Hero(), switchOptions, SwitchType, Navbar(), Props, useShop() (+10 more)

### Community 25 - "react"
Cohesion: 0.15
Nodes (19): AccountForm(), AccountManagement(), AccountTable(), blank, filterAccounts(), adminPages, AdminWorkspace(), CategoryForm() (+11 more)

### Community 26 - "src/services/api.js"
Cohesion: 0.14
Nodes (18): Checkout(), money(), money(), OrderImage(), orderLabels, OrderPayment(), OrderProduct(), paymentLabels (+10 more)

### Community 27 - "KbsDbContext"
Cohesion: 0.08
Nodes (21): KbsDbContext, AiEvents, AiModules, Brands, CartItems, Categories, ChatConversations, ChatMessages (+13 more)

### Community 28 - "Product"
Cohesion: 0.08
Nodes (23): Product, Brand, BrandId, CartItems, Category, CategoryId, Color, ConnectionType (+15 more)

### Community 29 - "CartResponseDto"
Cohesion: 0.19
Nodes (8): CartController, ICartService, CartService, CartResponseDto, Items, TotalPrice, UpdateCartItemDto, Quantity

### Community 30 - "system_componentmodel_dataannotations"
Cohesion: 0.11
Nodes (8): UpdateUserRoleDto, RoleId, UpdateUserStatusDto, IsActive, KBS.Models.DTOs.Likelist, KBS.Models.DTOs.Common, KBS.Models.DTOs.Products, KBS.Models.DTOs.Users

### Community 31 - "KBS.Models.Entities"
Cohesion: 0.30
Nodes (4): LikelistTests, KBS.Models.DTOs.Authentication, KBS.Models.Entities, KBS.Tests

### Community 32 - "ProductResponseDto"
Cohesion: 0.09
Nodes (21): ProductResponseDto, BrandId, BrandName, CategoryId, CategoryName, Color, ConnectionType, CreatedAt (+13 more)

### Community 33 - "ai_auto.py"
Cohesion: 0.15
Nodes (5): decision(), main(), post(), run_once(), AutoTests

### Community 34 - "compilerOptions"
Cohesion: 0.10
Nodes (19): compilerOptions, allowImportingTsExtensions, allowJs, isolatedModules, jsx, lib, module, moduleDetection (+11 more)

### Community 35 - "src_backup/smoke-render.mjs"
Cohesion: 0.16
Nodes (6): react-dom@^19.2.8, vite@^8.3.0, root, root, react-dom, vite

### Community 37 - "Brand"
Cohesion: 0.15
Nodes (11): AddCartItemDto, ProductId, Quantity, Brand, CreatedAt, Description, Id, IsActive (+3 more)

### Community 38 - "InventoryManagement.jsx"
Cohesion: 0.26
Nodes (14): csv, items, root, emptyFilters, InventoryManagement(), InventoryTable(), modeLabels, StockDetails() (+6 more)

### Community 39 - "compilerOptions"
Cohesion: 0.12
Nodes (16): compilerOptions, allowImportingTsExtensions, allowJs, isolatedModules, lib, module, moduleDetection, moduleResolution (+8 more)

### Community 40 - "http"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 41 - "ProductUpdateDto"
Cohesion: 0.12
Nodes (16): ProductUpdateDto, BrandId, CategoryId, Color, ConnectionType, Description, Images, IsActive (+8 more)

### Community 42 - "StaffResponses.cs"
Cohesion: 0.13
Nodes (15): ChatMessageDto, Content, CreatedAt, Id, ReadAt, SenderId, SenderName, OrderNoteDto (+7 more)

### Community 43 - "ProductCreateDto"
Cohesion: 0.13
Nodes (15): ProductCreateDto, BrandId, CategoryId, Color, ConnectionType, Description, Images, IsHotSwap (+7 more)

### Community 44 - "Order"
Cohesion: 0.13
Nodes (14): Order, CreatedAt, CustomerName, Id, OrderDetails, OrderStatus, PaymentMethod, PaymentStatus (+6 more)

### Community 45 - "InventoryResponseDto"
Cohesion: 0.25
Nodes (8): InventoryController, IInventoryService, InventoryService, InventoryResponseDto, Id, ProductId, StockQuantity, UpdatedAt

### Community 46 - ".Likes_are_unique_private_and_keep_unavailabl"
Cohesion: 0.27
Nodes (3): LikelistController, ILikelistService, LikelistService

### Community 47 - "InventoryMovement"
Cohesion: 0.14
Nodes (13): InventoryMovement, Actor, ActorId, CreatedAt, Delta, Id, Order, OrderId (+5 more)

### Community 48 - "User"
Cohesion: 0.14
Nodes (13): User, CartItems, CreatedAt, Email, FullName, Id, IsActive, Orders (+5 more)

### Community 52 - "StockAdjustmentRequest"
Cohesion: 0.15
Nodes (12): ChatReadRequest, ThroughMessageId, ChatSendRequest, Content, StockAdjustmentRequest, Delta, ExpectedQuantity, Reason (+4 more)

### Community 53 - "StaffSummaryDto"
Cohesion: 0.15
Nodes (13): StaffActivityDto, CreatedAt, Delta, Id, ProductName, Reason, StaffSummaryDto, LowStockCount (+5 more)

### Community 55 - "StaffInventoryDto"
Cohesion: 0.17
Nodes (12): StaffInventoryDto, CategoryId, CategoryName, Description, IsActive, LowStockThreshold, Name, OrderedQuantity (+4 more)

### Community 56 - "TestApiFactory"
Cohesion: 0.38
Nodes (3): Program, TestApiFactory, StaffWorkflowTests

### Community 57 - "KBS.Models.DTOs.Orders"
Cohesion: 0.18
Nodes (4): CreateOrderItemDto, ProductId, Quantity, KBS.Models.DTOs.Orders

### Community 58 - "LikelistItemResponseDto"
Cohesion: 0.18
Nodes (11): LikelistItemResponseDto, BrandName, CategoryName, CreatedAt, Images, IsActive, OriginalPrice, Price (+3 more)

### Community 59 - "src/pages/admin/Dashboard.jsx"
Cohesion: 0.35
Nodes (9): ConfirmBankPayment(), amount(), Dashboard(), DashboardContent(), DashboardDetails(), DashboardWorkspace(), dayLabel(), metrics (+1 more)

### Community 60 - ".OnModelCreating()"
Cohesion: 0.20
Nodes (7): AiMapping, StaffShift, EndedAt, Id, Staff, StaffId, StartedAt

### Community 61 - "InventoryMovementDto"
Cohesion: 0.20
Nodes (10): InventoryMovementDto, ActorId, ActorName, CreatedAt, Delta, Id, OrderId, QuantityAfter (+2 more)

### Community 62 - "StaffCustomerDto"
Cohesion: 0.20
Nodes (10): StaffCustomerDto, CreatedAt, Email, FullName, Id, IsActive, OrderCount, PhoneNumber (+2 more)

### Community 63 - "ChatMessage"
Cohesion: 0.20
Nodes (9): ChatMessage, Content, Conversation, ConversationId, CreatedAt, Id, ReadAt, Sender (+1 more)

### Community 64 - "ProductReview"
Cohesion: 0.20
Nodes (9): ProductReview, Comment, CreatedAt, Id, Product, ProductId, Rating, User (+1 more)

### Community 65 - "Role"
Cohesion: 0.22
Nodes (5): Role, Description, Id, Name, Users

### Community 66 - "ShopProvider.tsx"
Cohesion: 0.27
Nodes (5): App(), PublicShop, ShopContext, ShopProvider(), API_URL

### Community 67 - ".Reviews_require_completed_purchase_validate_"
Cohesion: 0.31
Nodes (5): ProductReviewService, ProductReviewCreateDto, Comment, Rating, ProductDetailTests

### Community 68 - "CartItemResponseDto"
Cohesion: 0.22
Nodes (9): CartItemResponseDto, Color, ImageUrl, Layout, ProductId, ProductName, Quantity, Subtotal (+1 more)

### Community 69 - "ChatConversationDto"
Cohesion: 0.22
Nodes (9): ChatConversationDto, CustomerId, CustomerName, Id, LastMessage, LastMessageAt, StaffId, StaffName (+1 more)

### Community 70 - "CartItem"
Cohesion: 0.22
Nodes (8): CartItem, Id, Product, ProductId, Quantity, UpdatedAt, User, UserId

### Community 71 - "ChatConversation"
Cohesion: 0.22
Nodes (8): ChatConversation, CreatedAt, Customer, CustomerId, Id, Messages, Staff, StaffId

### Community 72 - "OrderDetail"
Cohesion: 0.22
Nodes (8): OrderDetail, Id, Order, OrderId, Product, ProductId, Quantity, UnitPrice

### Community 73 - "OrderNote"
Cohesion: 0.22
Nodes (8): OrderNote, Content, CreatedAt, Id, Order, OrderId, Staff, StaffId

### Community 74 - "InventoryService.cs"
Cohesion: 0.25
Nodes (4): UpdateInventoryDto, Reason, StockQuantity, KBS.Models.DTOs.Inventory

### Community 75 - "Category"
Cohesion: 0.25
Nodes (7): Category, CreatedAt, Description, Id, IsActive, Name, Products

### Community 76 - "Inventory"
Cohesion: 0.25
Nodes (7): Inventory, Id, LowStockThreshold, Product, ProductId, StockQuantity, UpdatedAt

### Community 77 - "LikelistItem"
Cohesion: 0.25
Nodes (7): LikelistItem, CreatedAt, Id, Product, ProductId, User, UserId

### Community 78 - "ProductImage"
Cohesion: 0.25
Nodes (7): ProductImage, AltText, Id, ImageUrl, Product, ProductId, SortOrder

### Community 80 - "scripts"
Cohesion: 0.29
Nodes (7): scripts, build, dev, lint, preview, test:smoke, typecheck

### Community 81 - "ShopManagement.jsx"
Cohesion: 0.43
Nodes (5): homeFields, changed(), fields, formFrom(), ShopManagement()

### Community 82 - "ApiResponse"
Cohesion: 0.33
Nodes (5): ApiResponse, Data, Errors, Message, Success

### Community 83 - "ProductImageDto"
Cohesion: 0.33
Nodes (5): ProductImageDto, AltText, Id, ImageUrl, SortOrder

### Community 84 - "StaffReviewDto"
Cohesion: 0.33
Nodes (6): StaffReviewDto, Comment, CreatedAt, Id, ProductId, Rating

### Community 85 - ".oxlintrc.json"
Cohesion: 0.33
Nodes (5): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema

### Community 86 - "React storefront architecture"
Cohesion: 0.40
Nodes (5): Database-backed UI and engineering rules, TypeScript frontend entrypoint, Google Fonts typography, React storefront architecture, Backup JSX entrypoint

### Community 87 - "BrandUpdateDto"
Cohesion: 0.40
Nodes (4): BrandUpdateDto, Description, IsActive, Name

### Community 88 - "dependencies"
Cohesion: 0.40
Nodes (5): dependencies, lucide-react@^0.446.0, react@^19.2.8, @supabase/supabase-js@^2.57.4, @supabase/supabase-js

## Knowledge Gaps
- **542 isolated node(s):** `net9.0`, `Microsoft.AspNetCore.OpenApi`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Authentication.JwtBearer` (+537 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 699 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **24 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `KbsDbContext` connect `KbsDbContext` to `ShopSettings`, `AuthService`, `OrderResponseDto`, `AiModule`, `CategoryResponseDto`, `CancellationToken`, `UserResponseDto`, `microsoft_entityframeworkcore`, `Fact`, `DashboardResponseDto`, `BrandResponseDto`, `Product`, `CartResponseDto`, `ChatController`, `Brand`, `Order`, `InventoryResponseDto`, `.Likes_are_unique_private_and_keep_unavailabl`, `InventoryMovement`, `User`, `ProductService`, `ShopSettingsService`, `TestApiFactory`, `.OnModelCreating()`, `ChatMessage`, `ProductReview`, `Role`, `.Reviews_require_completed_purchase_validate_`, `CartItem`, `ChatConversation`, `OrderDetail`, `OrderNote`, `Category`, `Inventory`, `LikelistItem`, `ProductImage`, `.Settings_are_admin_only_public_fields_are_sa`, `.Admin_crud_validates_duplicates_protects_sel`?**
  _High betweenness centrality (0.063) - this node is a cross-community bridge._
- **Are the 5 inferred relationships involving `KbsDbContext` (e.g. with `.Category_lifecycle_validates_names_counts_products_and_protects_used_categories()` and `.Likes_are_unique_private_and_keep_unavailable_products_visible()`) actually correct?**
  _`KbsDbContext` has 5 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net9.0`, `Microsoft.AspNetCore.OpenApi`, `Npgsql.EntityFrameworkCore.PostgreSQL` to the rest of the system?**
  _542 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ShopSettings` be split into smaller, more focused modules?**
  _Cohesion score 0.05245901639344262 - nodes in this community are weakly interconnected._
- **Why does `Product` connect `Product` to `OrderResponseDto`, `CategoryResponseDto`, `DateTimeOffset`, `KbsDbContext`, `Brand`, `.Likes_are_unique_private_and_keep_unavailabl`, `InventoryMovement`, `ProductService`, `TestApiFactory`, `.OnModelCreating()`, `ProductReview`, `Role`, `.Reviews_require_completed_purchase_validate_`, `CartItem`, `OrderDetail`, `Category`, `Inventory`, `LikelistItem`, `ProductImage`?**
  _High betweenness centrality (0.030) - this node is a cross-community bridge._
- **Should `AuthService` be split into smaller, more focused modules?**
  _Cohesion score 0.05727644652250146 - nodes in this community are weakly interconnected._
- **Why does `ProductResponseDto` connect `ProductResponseDto` to `CancellationToken`, `HttpGet`, `Fact`, `ProductImageDto`, `ProductService`, `DateTimeOffset`?**
  _High betweenness centrality (0.027) - this node is a cross-community bridge._