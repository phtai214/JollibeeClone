using Microsoft.AspNetCore.Mvc.Rendering;

namespace JollibeeClone.Areas.Admin.ViewModels
{
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;
        public int StartIndex => (CurrentPage - 1) * PageSize + 1;
        public int EndIndex => Math.Min(StartIndex + PageSize - 1, TotalItems);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int? PreviousPage => HasPreviousPage ? CurrentPage - 1 : null;
        public int? NextPage => HasNextPage ? CurrentPage + 1 : null;

        public List<int> GetPageNumbers(int maxPages = 10)
        {
            var pages = new List<int>();
            var startPage = Math.Max(1, CurrentPage - maxPages / 2);
            var endPage = Math.Min(TotalPages, startPage + maxPages - 1);

            if (endPage - startPage + 1 < maxPages)
            {
                startPage = Math.Max(1, endPage - maxPages + 1);
            }

            for (int i = startPage; i <= endPage; i++)
            {
                pages.Add(i);
            }

            return pages;
        }
    }

    public class SearchFilterViewModel
    {
        public string? SearchString { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; } = "asc";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<FilterOption> FilterOptions { get; set; } = new();
    }

    public class FilterOption
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class BreadcrumbViewModel
    {
        public List<BreadcrumbItem> Items { get; set; } = new();
    }

    public class BreadcrumbItem
    {
        public string Text { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Icon { get; set; }
        public bool IsActive { get; set; }
    }

    public class AlertViewModel
    {
        public string Message { get; set; } = string.Empty;
        public AlertType Type { get; set; }
        public bool IsDismissible { get; set; } = true;
        public string? Title { get; set; }
        public string? Icon { get; set; }
        public int? AutoHideAfter { get; set; }
    }

    public class ModalViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public ModalSize Size { get; set; } = ModalSize.Medium;
        public bool ShowCloseButton { get; set; } = true;
        public List<ModalButton> Buttons { get; set; } = new();
    }

    public class ModalButton
    {
        public string Text { get; set; } = string.Empty;
        public string CssClass { get; set; } = "btn-secondary";
        public string? OnClick { get; set; }
        public bool CloseModal { get; set; } = true;
        public string? Icon { get; set; }
    }

    public class TableColumn
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsSortable { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public string? Width { get; set; }
        public string? CssClass { get; set; }
        public TableColumnType Type { get; set; } = TableColumnType.Text;
        public string? Format { get; set; }
    }

    public class DataTableViewModel<T>
    {
        public List<T> Data { get; set; } = new();
        public List<TableColumn> Columns { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();
        public SearchFilterViewModel SearchFilter { get; set; } = new();
        public string? TableId { get; set; }
        public bool ShowCheckboxes { get; set; }
        public bool ShowActions { get; set; } = true;
        public List<TableAction> BulkActions { get; set; } = new();
        public List<TableAction> RowActions { get; set; } = new();
    }

    public class TableAction
    {
        public string Text { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string CssClass { get; set; } = "btn-outline-primary";
        public bool RequiresConfirmation { get; set; }
        public string? ConfirmationMessage { get; set; }
        public bool IsVisible { get; set; } = true;
    }

    public class FileUploadViewModel
    {
        public string? AllowedExtensions { get; set; }
        public long MaxFileSize { get; set; } = 5 * 1024 * 1024; // 5MB
        public bool AllowMultiple { get; set; }
        public string? AcceptTypes { get; set; }
        public string UploadUrl { get; set; } = string.Empty;
        public bool ShowPreview { get; set; } = true;
        public string? PlaceholderText { get; set; }
    }

    public class StatisticsCardViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? SubValue { get; set; }
        public string? Icon { get; set; }
        public string? IconColor { get; set; }
        public string? BackgroundColor { get; set; }
        public TrendDirection? Trend { get; set; }
        public string? TrendValue { get; set; }
        public string? TrendText { get; set; }
        public string? LinkUrl { get; set; }
        public string? LinkText { get; set; }
    }

    public class ChartViewModel
    {
        public string ChartId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public ChartType Type { get; set; }
        public List<string> Labels { get; set; } = new();
        public List<ChartDataset> Datasets { get; set; } = new();
        public int? Height { get; set; }
        public int? Width { get; set; }
        public bool ShowLegend { get; set; } = true;
        public bool IsResponsive { get; set; } = true;
    }

    public class ChartDataset
    {
        public string Label { get; set; } = string.Empty;
        public List<decimal> Data { get; set; } = new();
        public string? BackgroundColor { get; set; }
        public string? BorderColor { get; set; }
        public int BorderWidth { get; set; } = 1;
        public bool Fill { get; set; }
    }

    public enum AlertType
    {
        Primary,
        Secondary,
        Success,
        Danger,
        Warning,
        Info,
        Light,
        Dark
    }

    public enum ModalSize
    {
        Small,
        Medium,
        Large,
        ExtraLarge
    }

    public enum TableColumnType
    {
        Text,
        Number,
        Currency,
        Date,
        DateTime,
        Boolean,
        Image,
        Badge,
        Actions,
        Html
    }

    public enum TrendDirection
    {
        Up,
        Down,
        Stable
    }

    public enum ChartType
    {
        Line,
        Bar,
        Pie,
        Doughnut,
        Radar,
        PolarArea
    }
} 