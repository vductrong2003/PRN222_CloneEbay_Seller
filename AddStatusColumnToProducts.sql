-- Add Status column to Products table
ALTER TABLE Product 
ADD Status NVARCHAR(50) NULL DEFAULT 'Active';

-- Update existing products with default status based on inventory
UPDATE Product 
SET Status = CASE 
    WHEN EXISTS (
        SELECT 1 FROM Inventory 
        WHERE Inventory.ProductId = Product.Id 
        AND Inventory.Quantity > 0
    ) THEN 'Active'
    ELSE 'Inactive'
END;

-- Add check constraint for valid status values
ALTER TABLE Product
ADD CONSTRAINT CK_Product_Status 
CHECK (Status IN ('Active', 'Inactive', 'Draft', 'Scheduled', 'Ended'));