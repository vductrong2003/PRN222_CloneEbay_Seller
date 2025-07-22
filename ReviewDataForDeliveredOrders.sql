-- Insert sample data for CloneEbayDB
USE [CloneEbayDB]
GO

-- ...existing code...

-- Insert additional Reviews for Delivered Orders
-- Order #7 (Gaming Monitor) - Already has review from Alice (user 3)
-- Order #9 (iPhone + Nike Air Jordan) - iPhone already has review from Charlie (user 5), add review for Nike shoes
-- Order #4 (Designer Handbag) - Already has review from Alice (user 3)

-- Additional Reviews for delivered orders to make the review system more comprehensive
INSERT INTO [Review] ([productId], [reviewerId], [rating], [comment], [createdAt]) VALUES
-- Review for Nike Air Jordan from Order #9 by Charlie (user 5)
(3, 5, 4, 'Great quality sneakers! They fit perfectly and are very comfortable for basketball. Shipping was fast and packaging was excellent. Highly recommend this seller!', '2025-07-16 10:30:00'),

-- Additional review for Gaming Monitor from Order #7 by Alice (user 3) - more detailed
-- Note: We already have one review, but let's update it to be more comprehensive
-- We'll add a second review as if Alice bought multiple items or left additional feedback

-- Review for Coffee Maker from Order #2 by Bob (user 4) - this order is shipped but let's assume it gets delivered
(5, 4, 5, 'Excellent coffee maker! Makes perfect coffee every time. The programmable features are very convenient for busy mornings. Great value for money.', '2025-07-22 08:45:00'),

-- Review for Samsung Galaxy from Order #2 by Bob (user 4) 
(2, 4, 4, 'Very good phone with excellent camera. Battery life is impressive and the S Pen functionality is great for productivity. Fast shipping and well packaged.', '2025-07-22 09:15:00'),

-- Additional review for MacBook Pro from a hypothetical delivered order
-- Let's add this assuming Order #5 gets delivered
(4, 4, 5, 'Outstanding laptop! The M3 Max chip is incredibly fast for video editing and development work. Screen quality is amazing and build quality is top-notch. Worth every penny!', '2025-07-19 16:30:00'),

-- Review for Wireless Headphones from Order #6 by Charlie (user 5)
(6, 5, 4, 'Good noise-canceling headphones. Sound quality is great for the price. Comfortable to wear for long periods. Bluetooth connection is stable.', '2025-07-24 11:20:00'),

-- More detailed reviews for products that buyers might review after extended use
(1, 3, 5, 'Update after 1 week of use: Still loving this iPhone! The camera system is incredible, especially for night photography. iOS updates are smooth and performance is flawless.', '2025-07-28 14:45:00'),

(7, 3, 5, 'Perfect handbag for daily use! The leather quality is exceptional and it matches perfectly with my outfits. Seller communication was excellent throughout the process.', '2025-07-22 12:10:00'),

-- Reviews from other buyers for variety
(8, 5, 5, 'Amazing gaming monitor! 4K resolution and 144Hz refresh rate make gaming incredibly smooth. HDR support makes colors pop. Setup was easy and seller provided great support.', '2025-07-18 15:45:00'),

(1, 4, 4, 'Great iPhone overall. Camera is excellent and battery lasts all day. Only minor complaint is the price, but quality justifies it. Fast shipping and secure packaging.', '2025-07-21 13:30:00'),

(3, 3, 5, 'Perfect sneakers for basketball! Great grip on court and very comfortable. The design is classic and quality is as expected from this brand. Will buy again!', '2025-07-25 09:00:00');

-- Update order statuses to ensure we have delivered orders for testing
UPDATE [OrderTable] SET [status] = 'Delivered' WHERE [id] = 2; -- Samsung + Coffee Maker order
UPDATE [OrderTable] SET [status] = 'Delivered' WHERE [id] = 5; -- MacBook Pro order  
UPDATE [OrderTable] SET [status] = 'Delivered' WHERE [id] = 6; -- Wireless Headphones order

-- Add shipping info for newly delivered orders
INSERT INTO [ShippingInfo] ([orderId], [carrier], [trackingNumber], [status], [estimatedArrival]) VALUES
(2, 'FedEx', 'FDX123456789', 'Delivered', '2025-07-22 14:00:00'),
(6, 'UPS', 'UPS777888999', 'Delivered', '2025-07-24 10:30:00');

-- Update existing shipping info to delivered status
UPDATE [ShippingInfo] SET [status] = 'Delivered' WHERE [orderId] = 5;

GO

PRINT 'Additional review data for delivered orders inserted successfully!'
PRINT 'Orders with Delivered status and reviews:'
PRINT '- Order #2: Samsung Galaxy S24 Ultra + Coffee Maker (Bob - user 4)'
PRINT '- Order #4: Designer Handbag (Alice - user 3)' 
PRINT '- Order #5: MacBook Pro 16-inch (Bob - user 4)'
PRINT '- Order #6: Wireless Headphones (Charlie - user 5)'
PRINT '- Order #7: Gaming Monitor (Alice - user 3)'
PRINT '- Order #9: iPhone + Nike Air Jordan (Charlie - user 5)'