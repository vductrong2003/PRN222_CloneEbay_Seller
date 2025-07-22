-- Insert sample data for CloneEbayDB
USE [CloneEbayDB]
GO

-- Insert Categories
INSERT INTO [Category] ([name]) VALUES
('Electronics'),
('Clothing & Accessories'),
('Home & Garden'),
('Sports & Outdoors'),
('Books'),
('Toys & Hobbies'),
('Automotive'),
('Health & Beauty');

-- Insert Users (including sellers and buyers)
INSERT INTO [User] ([username], [email], [password], [role], [avatarURL]) VALUES
('john_seller', 'john@seller.com', 'hashed_password_123', 'Seller', 'https://example.com/avatar1.jpg'),
('mary_seller', 'mary@seller.com', 'hashed_password_456', 'Seller', 'https://example.com/avatar2.jpg'),
('alice_buyer', 'alice@buyer.com', 'hashed_password_789', 'Buyer', 'https://example.com/avatar3.jpg'),
('bob_buyer', 'bob@buyer.com', 'hashed_password_101', 'Buyer', 'https://example.com/avatar4.jpg'),
('charlie_buyer', 'charlie@buyer.com', 'hashed_password_202', 'Buyer', 'https://example.com/avatar5.jpg'),
('david_seller', 'david@seller.com', 'hashed_password_303', 'Seller', 'https://example.com/avatar6.jpg');

-- Insert Stores
INSERT INTO [Store] ([sellerId], [storeName], [description], [bannerImageURL]) VALUES
(1, 'TechWorld Store', 'Your one-stop shop for all electronics and gadgets', 'https://example.com/banner1.jpg'),
(2, 'Fashion Hub', 'Latest trends in clothing and accessories', 'https://example.com/banner2.jpg'),
(6, 'Home Essentials', 'Everything you need for your home and garden', 'https://example.com/banner3.jpg');

-- Insert Products
INSERT INTO [Product] ([title], [description], [price], [images], [categoryId], [sellerId], [isAuction], [auctionEndTime]) VALUES
('iPhone 15 Pro Max', 'Latest iPhone with advanced camera system', 1199.99, 'https://example.com/iphone1.jpg,https://example.com/iphone2.jpg', 1, 1, 0, NULL),
('Samsung Galaxy S24 Ultra', 'Flagship Android phone with S Pen', 1299.99, 'https://example.com/samsung1.jpg,https://example.com/samsung2.jpg', 1, 1, 0, NULL),
('Nike Air Jordan Sneakers', 'Classic basketball shoes in excellent condition', 150.00, 'https://example.com/jordan1.jpg,https://example.com/jordan2.jpg', 2, 2, 1, '2025-07-25 23:59:59'),
('MacBook Pro 16-inch', 'M3 Max chip, 32GB RAM, 1TB SSD', 2499.99, 'https://example.com/macbook1.jpg,https://example.com/macbook2.jpg', 1, 1, 0, NULL),
('Coffee Maker Deluxe', 'Professional grade coffee maker with multiple settings', 299.99, 'https://example.com/coffee1.jpg', 3, 6, 0, NULL),
('Wireless Headphones', 'Noise-canceling bluetooth headphones', 199.99, 'https://example.com/headphones1.jpg,https://example.com/headphones2.jpg', 1, 1, 1, '2025-07-26 18:00:00'),
('Designer Handbag', 'Luxury leather handbag, brand new', 450.00, 'https://example.com/handbag1.jpg,https://example.com/handbag2.jpg', 2, 2, 0, NULL),
('Gaming Monitor 27inch', '4K 144Hz gaming monitor with HDR', 699.99, 'https://example.com/monitor1.jpg', 1, 1, 0, NULL);

-- Insert Addresses
INSERT INTO [Address] ([userId], [fullName], [phone], [street], [city], [state], [country], [isDefault]) VALUES
(3, 'Alice Johnson', '+1-555-0123', '123 Main Street', 'New York', 'NY', 'USA', 1),
(4, 'Bob Wilson', '+1-555-0456', '456 Oak Avenue', 'Los Angeles', 'CA', 'USA', 1),
(5, 'Charlie Brown', '+1-555-0789', '789 Pine Road', 'Chicago', 'IL', 'USA', 1),
(3, 'Alice Johnson Work', '+1-555-0124', '321 Business Blvd', 'New York', 'NY', 'USA', 0),
(4, 'Bob Wilson Home', '+1-555-0457', '654 Elm Street', 'Los Angeles', 'CA', 'USA', 0);

-- Insert Orders
INSERT INTO [OrderTable] ([buyerId], [addressId], [orderDate], [totalPrice], [status]) VALUES
(3, 1, '2025-07-20 10:30:00', 1199.99, 'Paid'),
(4, 2, '2025-07-21 14:15:00', 1599.98, 'Shipped'),
(5, 3, '2025-07-22 09:45:00', 299.99, 'Pending'),
(3, 1, '2025-07-19 16:20:00', 450.00, 'Delivered'),
(4, 2, '2025-07-18 11:10:00', 2499.99, 'Shipped'),
(5, 3, '2025-07-23 08:30:00', 199.99, 'Paid'),
(3, 4, '2025-07-17 13:45:00', 699.99, 'Delivered'),
(4, 5, '2025-07-16 12:00:00', 150.00, 'Cancelled'),
(5, 3, '2025-07-15 15:30:00', 849.98, 'Delivered');

-- Insert Order Items
INSERT INTO [OrderItem] ([orderId], [productId], [quantity], [unitPrice]) VALUES
(1, 1, 1, 1199.99),  -- iPhone 15 Pro Max
(2, 2, 1, 1299.99),  -- Samsung Galaxy S24 Ultra
(2, 5, 1, 299.99),   -- Coffee Maker
(3, 5, 1, 299.99),   -- Coffee Maker
(4, 7, 1, 450.00),   -- Designer Handbag
(5, 4, 1, 2499.99),  -- MacBook Pro
(6, 6, 1, 199.99),   -- Wireless Headphones
(7, 8, 1, 699.99),   -- Gaming Monitor
(8, 3, 1, 150.00),   -- Nike Air Jordan
(9, 1, 1, 1199.99),  -- iPhone 15 Pro Max
(9, 3, 1, 150.00);   -- Nike Air Jordan (cancelled order but keep items)

-- Insert Payments
INSERT INTO [Payment] ([orderId], [userId], [amount], [method], [status], [paidAt]) VALUES
(1, 3, 1199.99, 'Credit Card', 'Completed', '2025-07-20 10:35:00'),
(2, 4, 1599.98, 'PayPal', 'Completed', '2025-07-21 14:20:00'),
(4, 3, 450.00, 'Credit Card', 'Completed', '2025-07-19 16:25:00'),
(5, 4, 2499.99, 'Bank Transfer', 'Completed', '2025-07-18 11:15:00'),
(6, 5, 199.99, 'Credit Card', 'Completed', '2025-07-23 08:35:00'),
(7, 3, 699.99, 'PayPal', 'Completed', '2025-07-17 13:50:00'),
(8, 4, 150.00, 'Credit Card', 'Refunded', '2025-07-16 12:05:00'),
(9, 5, 849.98, 'Credit Card', 'Completed', '2025-07-15 15:35:00');

-- Insert Shipping Info
INSERT INTO [ShippingInfo] ([orderId], [carrier], [trackingNumber], [status], [estimatedArrival]) VALUES
(2, 'FedEx', 'FDX123456789', 'In Transit', '2025-07-24 17:00:00'),
(4, 'UPS', 'UPS987654321', 'Delivered', '2025-07-21 16:30:00'),
(5, 'USPS', 'USPS555666777', 'In Transit', '2025-07-25 14:00:00'),
(7, 'FedEx', 'FDX111222333', 'Delivered', '2025-07-19 10:15:00'),
(9, 'UPS', 'UPS444555666', 'Delivered', '2025-07-17 13:20:00');

-- Insert Inventory
INSERT INTO [Inventory] ([productId], [quantity], [lastUpdated]) VALUES
(1, 5, '2025-07-23 00:00:00'),
(2, 3, '2025-07-23 00:00:00'),
(3, 1, '2025-07-23 00:00:00'),
(4, 2, '2025-07-23 00:00:00'),
(5, 10, '2025-07-23 00:00:00'),
(6, 8, '2025-07-23 00:00:00'),
(7, 4, '2025-07-23 00:00:00'),
(8, 6, '2025-07-23 00:00:00');

-- Insert Feedback
INSERT INTO [Feedback] ([sellerId], [averageRating], [totalReviews], [positiveRate]) VALUES
(1, 4.8, 156, 96.15),
(2, 4.6, 89, 93.26),
(6, 4.9, 45, 97.78);

-- Insert some Reviews
INSERT INTO [Review] ([productId], [reviewerId], [rating], [comment], [createdAt]) VALUES
(1, 3, 5, 'Amazing phone! Great camera quality and battery life.', '2025-07-21 10:00:00'),
(7, 3, 5, 'Beautiful handbag, exactly as described. Fast shipping!', '2025-07-20 15:30:00'),
(4, 4, 5, 'Excellent laptop for professional work. Highly recommended!', '2025-07-19 09:45:00'),
(8, 3, 4, 'Great monitor for gaming. Colors are vibrant.', '2025-07-18 14:20:00'),
(1, 5, 5, 'Best iPhone yet! Worth the upgrade.', '2025-07-17 11:10:00');

-- Insert Bids for auction items
INSERT INTO [Bid] ([productId], [bidderId], [amount], [bidTime]) VALUES
(3, 3, 125.00, '2025-07-23 10:00:00'),
(3, 4, 135.00, '2025-07-23 11:30:00'),
(3, 5, 145.00, '2025-07-23 13:15:00'),
(6, 3, 175.00, '2025-07-23 09:00:00'),
(6, 4, 185.00, '2025-07-23 12:00:00'),
(6, 5, 190.00, '2025-07-23 14:30:00');

-- Insert Coupons
INSERT INTO [Coupon] ([code], [discountPercent], [startDate], [endDate], [maxUsage], [productId]) VALUES
('SAVE10', 10.00, '2025-07-20 00:00:00', '2025-07-30 23:59:59', 100, 1),
('TECH15', 15.00, '2025-07-22 00:00:00', '2025-08-01 23:59:59', 50, 2),
('NEWUSER20', 20.00, '2025-07-23 00:00:00', '2025-08-15 23:59:59', 200, NULL);

-- Insert Return Requests
INSERT INTO [ReturnRequest] ([orderId], [userId], [reason], [status], [createdAt]) VALUES
(4, 3, 'Item not as described. The handbag has some scratches that were not mentioned in the listing.', 'Approved', '2025-07-21 14:30:00'),
(7, 3, 'Changed my mind about the monitor. Would like to return within return period.', 'Processing', '2025-07-19 10:00:00'),
(9, 5, 'Item arrived damaged during shipping. Box was crushed and iPhone screen is cracked.', 'Approved', '2025-07-17 16:45:00');

-- Insert Disputes
INSERT INTO [Dispute] ([orderId], [raisedBy], [description], [status], [resolution]) VALUES
(2, 4, 'Order was marked as shipped but I never received tracking information. Seller is not responding to messages.', 'Open', NULL),
(5, 4, 'Received wrong model of MacBook. Ordered 16-inch but received 14-inch model.', 'Under Review', NULL),
(8, 4, 'Order was cancelled but refund has not been processed after 5 business days.', 'Resolved', 'Refund processed and completed. Customer notified via email.');

-- Update some orders to have Return/Dispute status
UPDATE [OrderTable] SET [status] = 'Returned' WHERE [id] = 4;
UPDATE [OrderTable] SET [status] = 'Disputed' WHERE [id] = 2;

GO

PRINT 'Sample data inserted successfully!'