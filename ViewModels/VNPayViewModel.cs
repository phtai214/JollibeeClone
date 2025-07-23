namespace JollibeeClone.ViewModels
{
    public class VNPayRequestModel
    {
        public string OrderCode { get; set; } = string.Empty;
        public string OrderDescription { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CreatedDate { get; set; }
        public string IpAddress { get; set; } = string.Empty;
    }

    public class VNPayResponseModel
    {
        public bool Success { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string OrderDescription { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string ResponseCode { get; set; } = string.Empty;
        public string TransactionStatus { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string PayDate { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class VNPayProcessingViewModel
    {
        public string OrderCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string OrderDescription { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
} 