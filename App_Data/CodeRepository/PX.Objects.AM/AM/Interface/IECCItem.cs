namespace PX.Objects.AM
{
	public interface IECCItem
	{
		string ID { get; set; }
		string RevisionID { get; set; }
		int? InventoryID { get; set; }
		int? SubItemID { get; set; }
		int? SiteID { get; set; }
		string Descr { get; set; }
		string BOMID { get; set; }
		string BOMRevisionID { get; set; }
	}
}
