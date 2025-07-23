-- Migration script to remove SellerRequest functionality and simplify User roles
-- Run this script to update your database schema

-- Drop SellerRequest table if it exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SellerRequest]') AND type in (N'U'))
BEGIN
    DROP TABLE [SellerRequest];
    PRINT 'SellerRequest table dropped successfully';
END
ELSE
BEGIN
    PRINT 'SellerRequest table does not exist';
END

-- Update User table schema
-- Ensure status column exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND name = 'status')
BEGIN
    ALTER TABLE [User] ADD [status] NVARCHAR(20) DEFAULT 'Active';
    PRINT 'Added status column to User table';
END

-- Ensure createdAt column exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND name = 'createdAt')
BEGIN
    ALTER TABLE [User] ADD [createdAt] DATETIME DEFAULT GETDATE();
    PRINT 'Added createdAt column to User table';
END

-- Ensure updatedAt column exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND name = 'updatedAt')
BEGIN
    ALTER TABLE [User] ADD [updatedAt] DATETIME;
    PRINT 'Added updatedAt column to User table';
END

-- Update existing data
-- Set default values for existing records
UPDATE [User] SET [status] = 'Active' WHERE [status] IS NULL;
UPDATE [User] SET [createdAt] = GETDATE() WHERE [createdAt] IS NULL;
UPDATE [User] SET [role] = 'Seller' WHERE [role] IS NULL OR [role] = 'User';

-- Update any Admin users to Seller (since we're removing Admin role)
UPDATE [User] SET [role] = 'Seller' WHERE [role] = 'Admin';

PRINT 'Updated existing user data - all users are now Sellers';

-- Create sample seller accounts if they don't exist
IF NOT EXISTS (SELECT * FROM [User] WHERE [email] = 'seller1@test.com')
BEGIN
    INSERT INTO [User] ([username], [email], [password], [role], [status], [createdAt])
    VALUES ('seller1', 'seller1@test.com', 'bqOzV6LWQRZ6AO9RYhU9/gJn+VhK0Mk8QL8H2CSWU6Y=', 'Seller', 'Active', GETDATE());
    PRINT 'Created test seller: seller1@test.com (password: admin123)';
END

IF NOT EXISTS (SELECT * FROM [User] WHERE [email] = 'seller2@test.com')
BEGIN
    INSERT INTO [User] ([username], [email], [password], [role], [status], [createdAt])
    VALUES ('seller2', 'seller2@test.com', 'bqOzV6LWQRZ6AO9RYhU9/gJn+VhK0Mk8QL8H2CSWU6Y=', 'Seller', 'Active', GETDATE());
    PRINT 'Created test seller: seller2@test.com (password: admin123)';
END

PRINT 'Migration completed successfully!';
PRINT 'Simplified system: All users are Sellers with Active status';
PRINT 'User roles: Seller (only role)';
PRINT 'User statuses: Active (default), Suspended';