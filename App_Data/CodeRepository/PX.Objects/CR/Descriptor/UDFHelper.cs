using System;
using System.Collections.Generic;
using PX.Objects.CS;
using System.Diagnostics;
using PX.Data;
using PX.CS;
using System.Linq;
using System.Collections;
using PX.Objects.CR.Extensions.CRCreateActions;
using PX.Data.MassProcess;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;

namespace PX.Objects.CR
{
	public static class UDFHelper
	{
		/// <summary>
		/// Return list of UDF fields for <paramref name="screenID"/> and <paramref name="udfTypeField"/>
		/// </summary>
		/// <param name="screenID"></param>
		/// <param name="udfTypeField"></param>
		/// <returns></returns>
		public static IEnumerable<PX.CS.KeyValueHelper.ScreenAttribute> GetUDFFields(string screenID, string udfTypeField = null)
		{
			List<string> attributes = new List<string>();
			foreach (var attr in KeyValueHelper.Def
									?.GetAttributes(screenID?.Replace(".", ""))
									?.Where(f =>
										(string.Equals(f.TypeValue, udfTypeField
												, StringComparison.CurrentCultureIgnoreCase)
										|| string.IsNullOrEmpty(f.TypeValue)))
									?.OrderBy(f => f.AttributeID)
									.ThenByDescending(f => f.TypeValue))
			{
				if (attributes.Contains(attr.AttributeID) == false)
				{
					attributes.Add(attr.AttributeID);
					yield return attr;
				}
			}
		}

		/// <summary>
		/// Return list of UDF fields for <paramref name="graphType"/> and <paramref name="udfTypeField"/>
		/// </summary>
		/// <param name="graphType"></param>
		/// <param name="udfTypeField"></param>
		/// <returns></returns>
		public static IEnumerable<PX.CS.KeyValueHelper.ScreenAttribute> GetUDFFields(Type graphType, string udfTypeField = null)
		{
			string screenID = PXSiteMap.Provider.FindSiteMapNode(graphType)?.ScreenID;
			return GetUDFFields(screenID, udfTypeField);
		}


		/// <summary>
		/// Return list of UDF fields for <paramref name="graph"/> and <paramref name="udfTypeField"/>
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="udfTypeField"></param>
		/// <returns></returns>
		public static IEnumerable<PX.CS.KeyValueHelper.ScreenAttribute> GetUDFFields(PXGraph graph, string udfTypeField = null)
		{
			return GetUDFFields(graph.Accessinfo.ScreenID, udfTypeField);
		}

		/// <summary>
		/// Return list of required UDF fields of <paramref name="targetGraph"/>
		/// </summary>
		/// <param name="sourceCache"></param>
		/// <param name="targetRow"></param>
		/// <param name="targetGraph"></param>
		/// <param name="udfTypeField"></param>
		/// <returns></returns>
		public static IEnumerable<PopupUDFAttributes> GetRequiredUDFFields(PXCache sourceCache, object targetRow, Type targetGraph, string udfTypeField = null)
		{
			var targetAttr = UDFHelper
								.GetUDFFields(targetGraph, udfTypeField)
								.Where(x => x.Required)
								.OrderBy(x => x.Column)
								.ThenBy(x => x.Row);
			string screenID = PXSiteMap.Provider.FindSiteMapNode(targetGraph)?.ScreenID;
			PXCache cache = sourceCache.Graph.Caches[typeof(PopupUDFAttributes)];
			PXCache destCache = sourceCache.Graph.Caches[GraphHelper.GetPrimaryCache(targetGraph.FullName).CacheType];

			foreach (var attr in targetAttr)
			{
				string val = "";
				val = sourceCache.Graph.Caches<PopupUDFAttributes>()
											.Cached
											.Cast<PopupUDFAttributes>()
											.Where(x =>
													x.AttributeID == attr.AttributeID
												&& x.ScreenID.Equals(screenID, StringComparison.CurrentCultureIgnoreCase)
											).FirstOrDefault()
										?.Value;
				if (string.IsNullOrEmpty(val))
				{
					val = (destCache.GetStateExt(targetRow, PX.CS.KeyValueHelper.AttributePrefix + attr.AttributeID) as PXFieldState)
							?.Value?.ToString();
					if (string.IsNullOrEmpty(val))
					{
						val = (destCache.GetStateExt(destCache.Current, PX.CS.KeyValueHelper.AttributePrefix + attr.AttributeID) as PXFieldState)
									?.Value?.ToString();
						if (string.IsNullOrEmpty(val))
						{
							val = (sourceCache.GetStateExt(sourceCache.Current, PX.CS.KeyValueHelper.AttributePrefix + attr.AttributeID) as PXFieldState)
										?.Value?.ToString();
							if (string.IsNullOrEmpty(val))
							{
								val = attr.DefaultValue;
							}
						}
					}
				}
				var field = new PopupUDFAttributes
					{
						Selected = false,
						CacheName = GraphHelper.GetPrimaryCache(targetGraph.FullName).Name,
						ScreenID = PXSiteMap.Provider.FindSiteMapNode(targetGraph)?.ScreenID,
						Name = attr.AttributeID,
						DisplayName = attr.Attribute.Description,
						AttributeID = attr.AttributeID,
						Value = val,
						Order = (attr.Column * 10000) + attr.Row,
						Required = attr.Required
					};

				var elem = (PopupUDFAttributes)cache.Locate(field);
				if (elem == null)
					cache.Hold(field);

				yield return elem ?? field;
			}
		}

		/// <summary>
		/// Fill <paramref name="destCache"/> by UDF values from <paramref name="sourceUDFPopupCache"/> values entered on pop-up dialog
		/// </summary>
		/// <param name="destCache"></param>
		/// <param name="sourceUDFPopupCache"></param>
		/// <param name="destRow"></param>
		/// <returns></returns>
		public static void FillfromPopupUDF(PXCache destCache, PXCache sourceUDFPopupCache, Type targetGraphType, object destRow)
		{
			string screenID = PXSiteMap.Provider.FindSiteMapNode(targetGraphType)?.ScreenID;
			foreach (PopupUDFAttributes attr in sourceUDFPopupCache.Cached as IEnumerable<PopupUDFAttributes>)
			{
				if (screenID?.Equals(attr.ScreenID, StringComparison.InvariantCultureIgnoreCase) == true)
				{
					destCache.SetValueExt(destRow, KeyValueHelper.AttributePrefix + attr.AttributeID, attr.Value);
				}
			}
		}

		/// <summary>
		/// Return PXFieldSatte for UDF <paramref name="attributeID"/> from <paramref name="graphType"/>
		/// </summary>
		/// <param name="graphType"></param>
		/// <param name="attributeID"></param>
		/// <returns></returns>
		public static PXFieldState GetGraphUDFFieldState(Type graphType, string attributeID)
		{
			string screenID = PXSiteMap.Provider.FindSiteMapNode(graphType)?.ScreenID;
			var state = PX.CS.KeyValueHelper.GetAttributeFields(screenID)
							.Where(a => a.Item1.Name.Equals(KeyValueHelper.AttributePrefix + attributeID, StringComparison.CurrentCultureIgnoreCase))
							.Select(x => x.Item1)
							.FirstOrDefault();
			return state;
		}

		/// <summary>
		/// Copy UDF from <paramref name="sourceCache"/> into <paramref name="destCache"/>
		/// </summary>
		/// <param name="sourceCache"></param>
		/// <param name="sourceData"></param>
		/// <param name="destCache"></param>
		/// <param name="destData"></param>
		/// <param name="udfTypeField"></param>
		/// <returns></returns>
		public static void CopyAttributes(PXCache sourceCache, object sourceData, PXCache destCache, object destData, string udfTypeField)
		{
			var targetAttr = UDFHelper.GetUDFFields(destCache.Graph.GetType(), udfTypeField);
			foreach (var attr in targetAttr)
			{
				string attribute = PX.CS.KeyValueHelper.AttributePrefix + attr.AttributeID;
				var val = (sourceCache.GetValueExt((sourceData ?? sourceCache.Current), attribute) as PXFieldState)?.Value;

				if (val == null || string.Empty.Equals(val))
				{
					var destVal = (destCache.GetValueExt(destCache.Current, attribute) as PXFieldState)?.Value;
					if (!(destVal == null || string.Empty.Equals(destVal)))
					{
						destCache.SetValueExt(destData, attribute, destVal);
					}
					else if (!string.IsNullOrEmpty(attr.DefaultValue))
					{
						destCache.SetValueExt(destData, attribute, attr.DefaultValue);
					}
				}
				else
				{
					destCache.SetValueExt(destData, attribute, val);
				}
			}
		}
	}
}
