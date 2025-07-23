using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using JollibeeClone.ViewModels;

namespace JollibeeClone.Services
{
    public class VNPayService
    {
        private readonly IConfiguration _configuration;

        public VNPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(VNPayRequestModel model)
        {
            try
            {
                Console.WriteLine($"🔍 VNPayService.CreatePaymentUrl called for order: {model.OrderCode}");
                
                var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(_configuration["TimeZoneId"]);
                var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
                // Use OrderCode as TxnRef so we can find the order in callback
                var txnRef = model.OrderCode;
                
                var vnpay = new VNPayLibrary();
                var urlCallBack = _configuration["PaymentCallBack:ReturnUrl"];
                var baseUrl = _configuration["VNPay:BaseUrl"];
                var hashSecret = _configuration["VNPay:HashSecret"];
                
                Console.WriteLine($"🔍 VNPay Configuration:");
                Console.WriteLine($"  - BaseUrl: {baseUrl}");
                Console.WriteLine($"  - ReturnUrl: {urlCallBack}");
                Console.WriteLine($"  - TmnCode: {_configuration["VNPay:TmnCode"]}");
                Console.WriteLine($"  - Amount: {model.Amount} -> {(int)(model.Amount * 100)}");
                Console.WriteLine($"  - CreateDate: {timeNow.ToString("yyyyMMddHHmmss")}");
                Console.WriteLine($"  - TxnRef (OrderCode): {txnRef}");
                Console.WriteLine($"  - IpAddress: {model.IpAddress}");

                // Validate required configuration
                if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(hashSecret) || string.IsNullOrEmpty(urlCallBack))
                {
                    throw new Exception("VNPay configuration is missing required values");
                }

                vnpay.AddRequestData("vnp_Version", _configuration["VNPay:Version"]);
                vnpay.AddRequestData("vnp_Command", _configuration["VNPay:Command"]);
                vnpay.AddRequestData("vnp_TmnCode", _configuration["VNPay:TmnCode"]);
                vnpay.AddRequestData("vnp_Amount", ((int)(model.Amount * 100)).ToString());
                vnpay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", _configuration["VNPay:CurrCode"]);
                vnpay.AddRequestData("vnp_IpAddr", model.IpAddress);
                vnpay.AddRequestData("vnp_Locale", _configuration["VNPay:Locale"]);
                vnpay.AddRequestData("vnp_OrderInfo", model.OrderDescription);
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", urlCallBack);
                vnpay.AddRequestData("vnp_TxnRef", txnRef);

                var paymentUrl = vnpay.CreateRequestUrl(baseUrl, hashSecret);
                
                Console.WriteLine($"🔍 Generated VNPay URL: {paymentUrl.Substring(0, Math.Min(200, paymentUrl.Length))}...");
                Console.WriteLine($"🔍 URL length: {paymentUrl.Length}");
                
                return paymentUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating VNPay URL: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public VNPayResponseModel ValidateCallback(IQueryCollection query)
        {
            try
            {
                Console.WriteLine($"🔍 VNPayService.ValidateCallback called");
                Console.WriteLine($"🔍 Query parameters count: {query.Count}");
                
                // Log all query parameters for debugging
                foreach (var item in query)
                {
                    Console.WriteLine($"🔍 Query param: {item.Key} = {item.Value}");
                }
                
                var hashSecret = _configuration["VNPay:HashSecret"];
                if (string.IsNullOrEmpty(hashSecret))
                {
                    Console.WriteLine($"❌ HashSecret is null or empty!");
                    return new VNPayResponseModel { Success = false, Message = "Missing HashSecret configuration" };
                }
                
                var vnpay = new VNPayLibrary();
                var response = vnpay.GetFullResponseData(query, hashSecret);
                
                Console.WriteLine($"🔍 VNPay validation result:");
                Console.WriteLine($"  - Success: {response.Success}");
                Console.WriteLine($"  - OrderId: {response.OrderId}");
                Console.WriteLine($"  - Amount: {response.Amount}");
                Console.WriteLine($"  - ResponseCode: {response.ResponseCode}");
                Console.WriteLine($"  - TransactionId: {response.TransactionId}");
                Console.WriteLine($"  - Message: {response.Message}");
                
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ValidateCallback: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return new VNPayResponseModel 
                { 
                    Success = false, 
                    Message = "Error validating callback: " + ex.Message 
                };
            }
        }


    }

    public class VNPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new();
        private readonly SortedList<string, string> _responseData = new();

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            try
            {
                Console.WriteLine($"🔍 VNPayLibrary.CreateRequestUrl called");
                Console.WriteLine($"🔍 BaseUrl: {baseUrl}");
                Console.WriteLine($"🔍 Request data count: {_requestData.Count}");
                
                // Validate inputs
                if (string.IsNullOrEmpty(baseUrl))
                    throw new ArgumentException("BaseUrl cannot be null or empty");
                if (string.IsNullOrEmpty(hashSecret))
                    throw new ArgumentException("HashSecret cannot be null or empty");
                
                var data = new StringBuilder();
                foreach (var kv in _requestData)
                {
                    if (!string.IsNullOrEmpty(kv.Value))
                    {
                        Console.WriteLine($"🔍 Adding param: {kv.Key} = {kv.Value}");
                        data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                    }
                }

                var queryString = data.ToString();
                Console.WriteLine($"🔍 Query string length: {queryString.Length}");

                if (string.IsNullOrEmpty(queryString))
                {
                    throw new Exception("No valid request data found");
                }

                baseUrl += "?" + queryString;
                var signData = queryString;
                if (signData.Length > 0)
                {
                    signData = signData.Remove(data.Length - 1, 1);
                }

                Console.WriteLine($"🔍 Sign data: {signData.Substring(0, Math.Min(100, signData.Length))}...");
                
                var vnp_SecureHash = HmacSHA512(hashSecret, signData);
                Console.WriteLine($"🔍 Generated hash: {vnp_SecureHash.Substring(0, 20)}...");
                
                baseUrl += "vnp_SecureHash=" + vnp_SecureHash;

                Console.WriteLine($"🔍 Final URL length: {baseUrl.Length}");
                return baseUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CreateRequestUrl: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            try
            {
                var rspRaw = GetResponseData();
                Console.WriteLine($"🔍 ValidateSignature:");
                Console.WriteLine($"  - Input hash: {inputHash}");
                Console.WriteLine($"  - Response data: {rspRaw.Substring(0, Math.Min(100, rspRaw.Length))}...");
                
                var myChecksum = HmacSHA512(secretKey, rspRaw);
                Console.WriteLine($"  - Calculated hash: {myChecksum}");
                
                var isValid = myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
                Console.WriteLine($"  - Signature valid: {isValid}");
                
                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ValidateSignature: {ex.Message}");
                return false;
            }
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }

        private string GetResponseData()
        {
            var data = new StringBuilder();
            if (_responseData.ContainsKey("vnp_SecureHashType"))
            {
                _responseData.Remove("vnp_SecureHashType");
            }

            if (_responseData.ContainsKey("vnp_SecureHash"))
            {
                _responseData.Remove("vnp_SecureHash");
            }

            foreach (var kv in _responseData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }

            return data.ToString();
        }

        public string GetResponseData(string key)
        {
            return _responseData.ContainsKey(key) ? _responseData[key] : string.Empty;
        }

        public VNPayResponseModel GetFullResponseData(IQueryCollection collection, string hashSecret)
        {
            try
            {
                Console.WriteLine($"🔍 VNPayLibrary.GetFullResponseData called");
                
                var vnPay = new VNPayLibrary();

                foreach (var (key, value) in collection)
                {
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    {
                        Console.WriteLine($"🔍 Adding response data: {key} = {value}");
                        vnPay.AddResponseData(key, value);
                    }
                }

                var orderId = vnPay.GetResponseData("vnp_TxnRef");
                var vnPayTranId = vnPay.GetResponseData("vnp_TransactionNo");
                var vnpResponseCode = vnPay.GetResponseData("vnp_ResponseCode");
                var vnpSecureHash = collection.FirstOrDefault(k => k.Key == "vnp_SecureHash").Value;
                var orderInfo = vnPay.GetResponseData("vnp_OrderInfo");
                var vnpAmount = vnPay.GetResponseData("vnp_Amount");
                var bankCode = vnPay.GetResponseData("vnp_BankCode");

                Console.WriteLine($"🔍 Extracted data:");
                Console.WriteLine($"  - OrderId: {orderId}");
                Console.WriteLine($"  - VnPayTranId: {vnPayTranId}");
                Console.WriteLine($"  - ResponseCode: {vnpResponseCode}");
                Console.WriteLine($"  - Amount: {vnpAmount}");
                Console.WriteLine($"  - BankCode: {bankCode}");
                Console.WriteLine($"  - SecureHash: {vnpSecureHash}");

                var checkSignature = vnPay.ValidateSignature(vnpSecureHash, hashSecret);
                Console.WriteLine($"🔍 Signature validation: {checkSignature}");

                if (!checkSignature)
                {
                    Console.WriteLine($"❌ Invalid signature detected!");
                    return new VNPayResponseModel()
                    {
                        Success = false,
                        Message = "Invalid signature",
                        OrderId = orderId,
                        ResponseCode = vnpResponseCode,
                        TransactionId = vnPayTranId
                    };
                }

                var isSuccess = vnpResponseCode == "00";
                var amount = !string.IsNullOrEmpty(vnpAmount) ? decimal.Parse(vnpAmount) / 100 : 0;
                
                Console.WriteLine($"🔍 Final result: Success={isSuccess}, Amount={amount}");

                return new VNPayResponseModel()
                {
                    Success = isSuccess,
                    PaymentMethod = "VnPay",
                    OrderDescription = orderInfo,
                    OrderId = orderId,
                    TransactionId = vnPayTranId,
                    ResponseCode = vnpResponseCode,
                    BankCode = bankCode,
                    Amount = amount,
                    Message = isSuccess ? "Thanh toán thành công" : GetResponseMessage(vnpResponseCode)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetFullResponseData: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return new VNPayResponseModel()
                {
                    Success = false,
                    Message = "Error processing response: " + ex.Message
                };
            }
        }

        private string GetResponseMessage(string responseCode)
        {
            return responseCode switch
            {
                "00" => "Giao dịch thành công",
                "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
                "09" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
                "10" => "Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
                "11" => "Giao dịch không thành công do: Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.",
                "12" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa.",
                "13" => "Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP). Xin quý khách vui lòng thực hiện lại giao dịch.",
                "24" => "Giao dịch không thành công do: Khách hàng hủy giao dịch",
                "51" => "Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.",
                "65" => "Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.",
                "75" => "Ngân hàng thanh toán đang bảo trì.",
                "79" => "Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định. Xin quý khách vui lòng thực hiện lại giao dịch",
                "99" => "Các lỗi khác (lỗi còn lại, không có trong danh sách mã lỗi đã liệt kê)",
                _ => "Giao dịch thất bại"
            };
        }
    }
} 