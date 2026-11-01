namespace QuickCartApi.DTOs.Voucher
{
    public class AppliedVoucher
    {
        public int VoucherId { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal Discount { get; set; }
    }
}
