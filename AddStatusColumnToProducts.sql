-- Add Status column to Products table
ALTER TABLE Products 
ADD Status NVARCHAR(50) NULL DEFAULT 'Active';

-- Update existing products with default status based on inventory
UPDATE Products 
SET Status = CASE 
    WHEN EXISTS (
        SELECT 1 FROM Inventories 
        WHERE Inventories.ProductId = Products.Id 
        AND Inventories.Quantity > 0
    ) THEN 'Active'
    ELSE 'Inactive'
END;

-- Add check constraint for valid status values
ALTER TABLE Products 
ADD CONSTRAINT CK_Products_Status 
CHECK (Status IN ('Active', 'Inactive', 'Draft', 'Scheduled', 'Ended'));