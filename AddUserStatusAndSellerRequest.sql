-- Migration script to add User Status, CreatedAt, UpdatedAt fields and SellerRequest table
-- Run this script to update your database schema

-- Add new columns to User table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND name = 'status')
BEGIN
    ALTER TABLE [User] ADD [status] NVARCHAR(20) DEFAULT 'Active';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND name = 'createdAt')
BEGIN
    ALTER TABLE [User] ADD [createdAt] DATETIME DEFAULT GETDATE();
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND name = 'updatedAt')
BEGIN
    ALTER TABLE [User] ADD [updatedAt] DATETIME;
END

-- Update existing User role default value
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND name = 'role')
BEGIN
    -- Update existing NULL roles to 'User'
    UPDATE [User] SET [role] = 'User' WHERE [role] IS NULL;
    
    -- Update existing NULL statuses to 'Active'
    UPDATE [User] SET [status] = 'Active' WHERE [status] IS NULL;
    
    -- Update existing NULL createdAt to current date
    UPDATE [User] SET [createdAt] = GETDATE() WHERE [createdAt] IS NULL;
END

-- Create SellerRequest table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SellerRequest]') AND type in (N'U'))
BEGIN
    CREATE TABLE [SellerRequest] (
        [id] INT IDENTITY(1,1) PRIMARY KEY,
        [userId] INT NOT NULL,
        [businessName] NVARCHAR(200) NOT NULL,
        [businessAddress] NVARCHAR(500) NOT NULL,
        [businessPhone] NVARCHAR(20) NOT NULL,
        [businessEmail] NVARCHAR(100) NOT NULL,
        [taxId] NVARCHAR(50),
        [requestReason] NVARCHAR(MAX) NOT NULL,
        [status] NVARCHAR(20) DEFAULT 'Pending',
        [adminNotes] NVARCHAR(MAX),
        [requestDate] DATETIME DEFAULT GETDATE(),
        [reviewedDate] DATETIME,
        [reviewedByAdminId] INT,
        
        CONSTRAINT FK_SellerRequest_User FOREIGN KEY ([userId]) REFERENCES [User]([id]),
        CONSTRAINT FK_SellerRequest_ReviewedByAdmin FOREIGN KEY ([reviewedByAdminId]) REFERENCES [User]([id])
    );
    
    PRINT 'SellerRequest table created successfully';
END
ELSE
BEGIN
    PRINT 'SellerRequest table already exists';
END

-- Insert sample data for testing (optional)
-- Create a test admin user if not exists
IF NOT EXISTS (SELECT * FROM [User] WHERE [email] = 'admin@cloneebay.com')
BEGIN
    INSERT INTO [User] ([username], [email], [password], [role], [status], [createdAt])
    VALUES ('admin', 'admin@cloneebay.com', 'bqOzV6LWQRZ6AO9RYhU9/gJn+VhK0Mk8QL8H2CSWU6Y=', 'Admin', 'Active', GETDATE());
    
    PRINT 'Test admin user created: admin@cloneebay.com (password: admin123)';
END

-- Create a test regular user if not exists
IF NOT EXISTS (SELECT * FROM [User] WHERE [email] = 'user@test.com')
BEGIN
    INSERT INTO [User] ([username], [email], [password], [role], [status], [createdAt])
    VALUES ('testuser', 'user@test.com', 'bqOzV6LWQRZ6AO9RYhU9/gJn+VhK0Mk8QL8H2CSWU6Y=', 'User', 'Active', GETDATE());
    
    PRINT 'Test user created: user@test.com (password: admin123)';
END

-- Create a test seller user if not exists
IF NOT EXISTS (SELECT * FROM [User] WHERE [email] = 'seller@test.com')
BEGIN
    INSERT INTO [User] ([username], [email], [password], [role], [status], [createdAt])
    VALUES ('testseller', 'seller@test.com', 'bqOzV6LWQRZ6AO9RYhU9/gJn+VhK0Mk8QL8H2CSWU6Y=', 'Seller', 'Active', GETDATE());
    
    PRINT 'Test seller created: seller@test.com (password: admin123)';
END

PRINT 'Migration completed successfully!';
PRINT 'User roles: User (default), Seller, Admin';
PRINT 'User statuses: Active (default), Pending, Suspended, Rejected';
PRINT 'SellerRequest statuses: Pending (default), Approved, Rejected';