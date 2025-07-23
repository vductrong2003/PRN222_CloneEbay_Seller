# CloneEbay Seller Platform - Simplified Seller Registration

## 📋 Tổng quan
Platform CloneEbay dành cho seller với hệ thống đăng ký đơn giản và quản lý sản phẩm hiệu quả.

## 🚀 Tính năng chính

### 1. Quản lý tài khoản:
- **Đăng ký**: Tự động trở thành seller khi đăng ký thành công
- **Đăng nhập**: Xác thực session-based
- **Quản lý profile**: Cập nhật thông tin cá nhân

### 2. Quản lý sản phẩm:
- **Tạo listing**: Đăng bán sản phẩm với đầy đủ thông tin
- **Chỉnh sửa**: Cập nhật thông tin sản phẩm
- **Quản lý trạng thái**: Active/Paused/Discontinued
- **Upload hình ảnh**: Hỗ trợ multiple images

### 3. Quản lý đơn hàng:
- **Theo dõi orders**: Xem đơn hàng của sản phẩm đã bán
- **Cập nhật trạng thái**: Processing → Shipped → Delivered
- **In shipping label**: Tạo nhãn gửi hàng

### 4. Báo cáo hiệu suất:
- **Doanh thu**: Theo ngày/tuần/tháng
- **Sản phẩm bán chạy**: Top selling products
- **Đánh giá**: Review và rating từ khách hàng

### 5. Quản lý store:
- **Thông tin store**: Tên, mô tả, logo
- **Chính sách**: Return, shipping policies
- **Branding**: Tùy chỉnh giao diện store

## 🛠️ Công nghệ sử dụng
- **Framework**: ASP.NET Core 8.0 MVC
- **Database**: SQL Server với Entity Framework Core
- **Frontend**: Bootstrap 5, jQuery
- **Session**: In-memory session management
- **Architecture**: Service-Repository pattern

## 📁 Cấu trúc dự án

### Controllers:
- **AccountController**: Authentication, user management
- **ListingController**: Product management (user-specific)
- **OrdersController**: Order management (seller's products only)
- **StoresController**: Store management (user-specific)
- **PerformanceController**: Analytics (user-specific)

### Services:
- **AccountService**: User authentication, seller verification
- **ListingService**: Product CRUD operations
- **OrderService**: Order processing with seller filtering
- **PerformanceService**: Analytics with seller filtering
- **StoreService**: Store management

### Models:
- **User**: User accounts với seller capabilities
- **Product**: Product listings
- **OrderTable**: Customer orders
- **Store**: Seller stores
- **Category**: Product categorization

## 🔧 Setup & Installation

### 1. Database Setup:
```sql
-- Run migration scripts in order:
1. SampleData.sql (Base data)
2. AddStatusColumnToProducts.sql (Product status)
3. RemoveSellerRequestMigration.sql (Remove old SellerRequest system)
4. ReviewDataForDeliveredOrders.sql (Review system)
```

### 2. Configuration:
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your SQL Server connection string"
  }
}
```

### 3. Run Application:
```bash
dotnet restore
dotnet build
dotnet run
```

## 🔑 Key Features

### Automatic Seller Registration:
- Users become sellers immediately upon registration
- No approval process required
- Instant access to all seller features

### User-Specific Data Filtering:
- All controllers filter data by current user
- Sellers only see their own products/orders/stores
- Secure session-based authentication

### Responsive Design:
- Mobile-friendly interface
- Bootstrap 5 styling
- Intuitive navigation

## 📊 Database Schema

### Core Tables:
- **User**: seller accounts and authentication
- **Product**: product listings with status
- **OrderTable**: customer orders
- **OrderItem**: order line items
- **Store**: seller store information
- **Category**: product categorization

### Key Relationships:
- User → Product (1:N) - Seller has many products
- User → Store (1:N) - Seller has many stores  
- Product → OrderItem (1:N) - Product in many orders
- OrderTable → OrderItem (1:N) - Order has many items

## 🚦 Development Status

✅ **Completed Features:**
- User registration/authentication
- Product management (CRUD)
- Order tracking
- Performance analytics
- Store management
- Responsive UI

🔄 **In Progress:**
- Advanced search/filtering
- Payment integration
- Email notifications
- Advanced analytics

📋 **Planned Features:**
- Multi-language support
- Advanced reporting
- Mobile app API
- Third-party integrations

## 🤝 Contributing
1. Fork the repository
2. Create feature branch
3. Commit changes
4. Push to branch
5. Create Pull Request

## 📝 License
This project is for educational purposes (PRN222 course).