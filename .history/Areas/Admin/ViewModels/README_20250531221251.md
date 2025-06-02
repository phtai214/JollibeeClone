# Admin ViewModels Documentation

Thư mục này chứa tất cả các ViewModel được sử dụng trong khu vực Admin Panel của ứng dụng Jollibee Clone.

## Cấu trúc ViewModels

### 1. **LoginViewModel.cs**
- `LoginViewModel`: ViewModel cho form đăng nhập admin
- Chứa validation cho email, password và remember me

### 2. **DashboardViewModel.cs**
- `DashboardViewModel`: ViewModel tổng hợp cho trang dashboard
- `DashboardStats`: Thống kê tổng quan
- `ChartDataPoint`: Dữ liệu cho biểu đồ
- `TopProduct`, `TopCategory`: Sản phẩm và danh mục bán chạy
- `DashboardAlert`, `DashboardNotification`: Thông báo và cảnh báo
- `RealTimeData`: Dữ liệu real-time
- `RecentActivity`: Hoạt động gần đây

### 3. **ProductViewModel.cs**
- `ProductCreateViewModel`: Tạo sản phẩm mới
- `ProductEditViewModel`: Chỉnh sửa sản phẩm
- `ProductListViewModel`: Danh sách sản phẩm với filter và pagination
- `ProductDetailsViewModel`: Chi tiết sản phẩm với thống kê

### 4. **CategoryViewModel.cs**
- `CategoryCreateViewModel`: Tạo danh mục mới
- `CategoryEditViewModel`: Chỉnh sửa danh mục
- `CategoryListViewModel`: Danh sách danh mục
- `CategoryDetailsViewModel`: Chi tiết danh mục với thống kê
- `CategoryHierarchyViewModel`: Cấu trúc cây danh mục
- `CategoryTreeNode`: Node trong cây danh mục

### 5. **UserViewModel.cs**
- `UserCreateViewModel`: Tạo người dùng mới
- `UserEditViewModel`: Chỉnh sửa người dùng
- `UserListViewModel`: Danh sách người dùng với filter
- `UserDetailsViewModel`: Chi tiết người dùng với thống kê
- `UserStatistics`: Thống kê người dùng
- `UserChangePasswordViewModel`: Đổi mật khẩu
- `UserRoleAssignmentViewModel`: Phân quyền
- `UserBulkActionViewModel`: Thao tác hàng loạt
- `UserExportViewModel`: Xuất dữ liệu

### 6. **OrderViewModel.cs**
- `OrderViewModel`: Danh sách đơn hàng
- `OrderUpdateStatusViewModel`: Cập nhật trạng thái đơn hàng
- `OrderStatisticsViewModel`: Thống kê đơn hàng
- `OrderStatusCount`: Thống kê theo trạng thái
- `DailyRevenueData`: Doanh thu theo ngày

### 7. **CommonViewModel.cs**
- `PaginationViewModel`: Phân trang
- `SearchFilterViewModel`: Tìm kiếm và lọc
- `BreadcrumbViewModel`: Breadcrumb navigation
- `AlertViewModel`: Thông báo
- `ModalViewModel`: Modal dialog
- `DataTableViewModel<T>`: Bảng dữ liệu generic
- `FileUploadViewModel`: Upload file
- `StatisticsCardViewModel`: Card thống kê
- `ChartViewModel`: Biểu đồ

## Quy tắc sử dụng

### 1. **Validation**
- Tất cả ViewModels đều có validation attributes
- Sử dụng DataAnnotations cho validation cơ bản
- Custom validation trong Controller khi cần thiết

### 2. **Naming Convention**
- ViewModels kết thúc bằng `ViewModel`
- Tên phản ánh chức năng: `ProductCreateViewModel`, `UserEditViewModel`
- Enums sử dụng PascalCase

### 3. **Structure**
- Properties có default values hợp lý
- Nullable properties cho optional fields
- Display attributes cho labels

### 4. **Dependencies**
- Import `JollibeeClone.Areas.Admin.Models` cho domain models
- Import `Microsoft.AspNetCore.Mvc.Rendering` cho SelectListItem
- Import `System.ComponentModel.DataAnnotations` cho validation

## Examples

### Sử dụng trong Controller:
```csharp
public async Task<IActionResult> Create()
{
    var viewModel = new ProductCreateViewModel();
    viewModel.Categories = await GetCategoriesSelectList();
    return View(viewModel);
}
```

### Sử dụng trong View:
```html
@model ProductCreateViewModel

<form asp-action="Create">
    <div class="form-group">
        <label asp-for="ProductName"></label>
        <input asp-for="ProductName" class="form-control" />
        <span asp-validation-for="ProductName" class="text-danger"></span>
    </div>
</form>
```

### Generic DataTable:
```csharp
var tableModel = new DataTableViewModel<Product>
{
    Data = products,
    Columns = GetProductColumns(),
    Pagination = new PaginationViewModel { /* ... */ }
};
```

## Notes

- ViewModels được tách biệt khỏi Domain Models để tăng tính linh hoạt
- Mỗi ViewModel phục vụ cho một mục đích cụ thể
- Có thể extend hoặc customize theo nhu cầu dự án
- Sử dụng composition thay vì inheritance khi có thể 