namespace PX.Objects.AP.InvoiceRecognition.Feedback.VendorSearch
{
	internal class Candidate
	{
		public float? Score { get; set; }
		public string Term { get; set; }
		public Emails Emails { get; set; }
	}
}
