using PX.Data;

namespace PX.Objects.CR
{
	#region ShowAsListAttribute

	public class ShowAsListAttribute : PXIntListAttribute
	{
		public const int Free = 1;
		public const int OutOfOffice = 3;
		public const int Busy = 2;
		public const int Tentative = 4;

		public ShowAsListAttribute()
			: base(
				new[]
				{
					Free,
					OutOfOffice,
					Busy,
					Tentative
				},
				new[]
				{
					Messages.Free,
					Messages.OutOfOffice,
					Messages.Busy,
					Messages.Tentative
				})
		{
		}

		public class free : PX.Data.BQL.BqlInt.Constant<free>
		{
			public free() : base(Free) { }
		}
		public class outOfOffice : PX.Data.BQL.BqlInt.Constant<outOfOffice>
		{
			public outOfOffice() : base(OutOfOffice) { }
		}
		public class busy : PX.Data.BQL.BqlInt.Constant<busy>
		{
			public busy() : base(Busy) { }
		}
		public class tentative : PX.Data.BQL.BqlInt.Constant<tentative>
		{
			public tentative() : base(Tentative) { }
		}
	}

	#endregion
}