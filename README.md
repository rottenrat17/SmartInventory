# Smart Inventory Management System

> **IMPORTANT NOTICE FOR PROFESSOR**: We are using the 'final' branch instead of 'main' to deploy the website. We removed email verification for registration as it was blocking new users from registering with Gmail accounts.

A dynamic, web-based inventory management system for small businesses, developed as part of COMP 2139 - Assignment 1.

## Features

- **Inventory Management**: Add, update, and delete products with details such as name, category, price, quantity, and low stock threshold.
- **Categories**: Organize products into different categories.
- **Search and Filtering**: Search for products by name and filter by category, price range, or low-stock status.
- **Guest Order Tracking**: Allow guests to place orders and track them without registration.
- **Low Stock Alerts**: Visual indicators for products with stock levels below the specified threshold.
- **User Registration**: Users can register with any email (including Gmail) without verification requirements.

## Technologies Used

- ASP.NET Core MVC (.NET 9.0)
- C#
- PostgreSQL Database
- Entity Framework Core
- Bootstrap 5
- jQuery

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- PostgreSQL

### Database Setup

1. Update the connection string in `appsettings.json` with your PostgreSQL credentials:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=SmartInventory;Username=yourUsername;Password=yourPassword"
}
```

2. Apply migrations to create the database:

```
dotnet ef database update
```

### Running the Application

1. Clone the repository
2. Navigate to the project directory
3. Run the application:

```
dotnet run
```

4. Access the application at `https://localhost:5001` or `http://localhost:5000`

## Project Structure

- **Models**: Product, Category, Order, OrderItem
- **Controllers**: Home, Products, Category, Order
- **Views**: Corresponding views for each controller
- **Database**: ApplicationDbContext with Entity Framework Core

## Assignment 2 Preview

This project will be expanded in Assignment 2 to include:
- User authentication and role-based access
- Detailed order tracking and history
- Analytics and reporting
- Administrative features

## Contributors

[Your Name(s) and Student ID(s)] 