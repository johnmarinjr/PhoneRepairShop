using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.IN;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Text;

namespace PX.Objects.FS
{
    public static class MessageHelper
    {
        public class ErrorInfo
        {
            public int? SOID;
			public int? SODetID;
			public int? AppointmentID;
			public int? AppDetID;
			public string ErrorMessage;
            public bool HeaderError;
        }

        public static int GetRowMessages(PXCache cache, object row, List<string> errors, List<string> warnings, bool includeRowInfo)
        {
            if (cache == null || row == null)
            {
                return 0;
            }

            int errorCount = 0;
            PXFieldState fieldState;

            foreach (string field in cache.Fields)
            {
				if (field.ToLower().StartsWith(typeof(FSAppointment.createdByID).Name.ToLower())
					|| field.ToLower().StartsWith(typeof(FSAppointment.lastModifiedByID).Name.ToLower()))
				{
					continue;
				}

                try
                {
                    fieldState = (PXFieldState)cache.GetStateExt(row, field);
                }
                catch
                {
                    fieldState = null;
                }

                if (fieldState != null && fieldState.Error != null)
                {
                    if (errors != null)
                    {
                        if (fieldState.ErrorLevel != PXErrorLevel.RowWarning
                            && fieldState.ErrorLevel != PXErrorLevel.Warning
                            && fieldState.ErrorLevel != PXErrorLevel.RowInfo
                        )
                        {
                            errors.Add(fieldState.Error);
                            errorCount++;
                        }
                    }

                    if (warnings != null)
                    {
                        if (fieldState.ErrorLevel == PXErrorLevel.RowWarning
                            || fieldState.ErrorLevel == PXErrorLevel.Warning
                            || (fieldState.ErrorLevel == PXErrorLevel.RowInfo && includeRowInfo == true)
                        )
                        {
                            warnings.Add(fieldState.Error);
                        }
                    }
                }
            }

            return errorCount;
        }

        public static string GetRowMessage(PXCache cache, IBqlTable row, bool getErrors, bool getWarnings)
        {
            List<string> errors = null;
            List<string> warnings = null;

            if (getErrors)
            {
                errors = new List<string>();
            }
            if (getWarnings)
            {
                warnings = new List<string>();
            }

            GetRowMessages(cache, row, errors, warnings, false);

            StringBuilder messageBuilder = new StringBuilder();

            if (errors != null)
            {
                foreach (string message in errors)
                {
                    if (messageBuilder.Length > 0)
                    {
                        messageBuilder.Append(Environment.NewLine);
                    }

                    messageBuilder.Append(message);
                }
            }

            if (warnings != null)
            {
                foreach (string message in warnings)
                {
                    if (messageBuilder.Length > 0)
                    {
                        messageBuilder.Append(Environment.NewLine);
                    }

                    messageBuilder.Append(message);
                }
            }

            return messageBuilder.ToString();
        }

        public static List<ErrorInfo> GetErrorInfo<TranType>(PXCache headerCache, IBqlTable headerRow, PXSelectBase<TranType> detailView, Type extensionType)
            where TranType : class, IBqlTable, new()
        {
            List<ErrorInfo> errorList = new List<ErrorInfo>();
            ErrorInfo errorInfo = null;

            string headerErrorMessage = MessageHelper.GetRowMessage(headerCache, headerRow, true, false);

            if (string.IsNullOrEmpty(headerErrorMessage) == false)
            {
                errorInfo = new ErrorInfo()
                {
                    HeaderError = true,
                    SOID = null,
					SODetID = null,
                    AppointmentID = null,
					AppDetID = null,
                    ErrorMessage = headerErrorMessage
                };

                errorList.Add(errorInfo);
            }

            foreach (object row in detailView.Select())
            {
				object localRow = row;
				IFSRelatedDoc rowExtension = null;

				if (typeof(TranType) == typeof(ARTran))
				{
					PXResult<ARTran, FSARTran> rLocalRow = (PXResult<ARTran, FSARTran>)row;
					localRow = (ARTran)rLocalRow;
					ARTran tranRow = (ARTran)rLocalRow;
					FSARTran fsARTranRow = new FSARTran { TranType = tranRow.TranType, RefNbr = tranRow.RefNbr, LineNbr = tranRow.LineNbr };
					rowExtension = (FSARTran)headerCache.Graph.Caches[typeof(FSARTran)].Locate(fsARTranRow);
				}
			
				string errorMessage = MessageHelper.GetRowMessage(detailView.Cache, (TranType)localRow, true, false);

                if (string.IsNullOrEmpty(errorMessage) == false)
                {
                    if (extensionType != null)
                    {
                        if (extensionType == typeof(SOLine))
                        {
                            rowExtension = detailView.Cache.GetExtension<FSxSOLine>(localRow);
                        }
                        else if (extensionType == typeof(INTran))
                        {
                            rowExtension = detailView.Cache.GetExtension<FSxINTran>(localRow);
                        }
                        else if (extensionType == typeof(APTran))
                        {
                            rowExtension = detailView.Cache.GetExtension<FSxAPTran>(localRow);
                        }
                        else if (extensionType == typeof(ARTran))
                        {
                        }
                        else 
                        {
                            errorInfo = new ErrorInfo()
                            {
                                HeaderError = false,
                                SOID = null,
								SODetID = null,
                                AppointmentID = null,
								AppDetID = null,
                                ErrorMessage = errorMessage + ", "
                            };

                            errorList.Add(errorInfo);
                        }

                        if (rowExtension != null && string.IsNullOrEmpty(rowExtension.SrvOrdType) == false) 
                        {
                            if (string.IsNullOrEmpty(rowExtension.AppointmentRefNbr) == false)
                            {
                                FSAppointment fsAppointmentRow = FSAppointment.PK.Find(headerCache.Graph, 
                                                                                        rowExtension.SrvOrdType, 
                                                                                        rowExtension.AppointmentRefNbr);

								FSAppointmentDet fsAppDetRow = FSAppointmentDet.PK.Find(headerCache.Graph,
																					rowExtension.SrvOrdType,
																					rowExtension.AppointmentRefNbr,
																					rowExtension.AppointmentLineNbr);

								errorInfo = new ErrorInfo()
                                {
                                    HeaderError = false,
                                    SOID = fsAppointmentRow.SOID,
									SODetID = null,
                                    AppointmentID = fsAppointmentRow?.AppointmentID,
									AppDetID = fsAppDetRow?.AppDetID,
                                    ErrorMessage = errorMessage + ", "
                                };

                                errorList.Add(errorInfo);
                            } 
                            else if (string.IsNullOrEmpty(rowExtension.ServiceOrderRefNbr) == false)
                            {
                                FSServiceOrder fsServiceOrderRow = FSServiceOrder.PK.Find(headerCache.Graph,
                                                                                            rowExtension.SrvOrdType,
                                                                                            rowExtension.ServiceOrderRefNbr);
								FSSODet fsSODetRow = FSSODet.PK.Find(headerCache.Graph,
																		rowExtension.SrvOrdType,
																		rowExtension.ServiceOrderRefNbr,
																		rowExtension.ServiceOrderLineNbr);

								errorInfo = new ErrorInfo()
                                {
                                    HeaderError = false,
                                    SOID = fsServiceOrderRow?.SOID,
									SODetID = fsSODetRow.SODetID,
									AppointmentID = null,
									AppDetID = null,
                                    ErrorMessage = errorMessage + ", "
                                };

                                errorList.Add(errorInfo);
                            }
                        }
                    }
                }
            }

            return errorList;
        }

        public static List<ErrorInfo> GetErrorInfo<TranType>(PXCache headerCache, IBqlTable headerRow, PXSelectBase<TranType> detailView)
            where TranType : class, IBqlTable, new()
        {
            List<ErrorInfo> errorList = new List<ErrorInfo>();
            ErrorInfo errorInfo = null;

            string headerErrorMessage = MessageHelper.GetRowMessage(headerCache, headerRow, true, false);

            if (string.IsNullOrEmpty(headerErrorMessage) == false)
            {
                errorInfo = new ErrorInfo()
                {
                    HeaderError = true,
                    SOID = null,
					SODetID = null,
                    AppointmentID = null,
					AppDetID = null,
                    ErrorMessage = headerErrorMessage
                };

                errorList.Add(errorInfo);
            }

            foreach (TranType row in detailView.Select())
            {
                string errorMessage = MessageHelper.GetRowMessage(detailView.Cache, row, true, false);

                if (string.IsNullOrEmpty(errorMessage) == false)
                {
                    errorInfo = new ErrorInfo()
                    {
                        HeaderError = false,
                        SOID = null,
						SODetID = null,
						AppointmentID = null,
						AppDetID = null,
						ErrorMessage = errorMessage + ", "
                    };

                    errorList.Add(errorInfo);
                }
            }

            return errorList;
        }

        public static string GetLineDisplayHint(PXGraph graph, string lineRefNbr, string lineDescr, int? inventoryID)
        {
            string strHintText = string.Empty;
            if (string.IsNullOrEmpty(lineRefNbr))
                return strHintText;

            strHintText = lineRefNbr;

            if (inventoryID != null)
            {
                InventoryItem item = InventoryItem.PK.Find(graph, inventoryID);
                if (item != null)
                {
                    strHintText += " - ";
                    strHintText += item.InventoryCD.Trim();
                }
            }

            if(string.IsNullOrEmpty(lineDescr) == false)
                strHintText += " (" + lineDescr.Trim() + ")";

            return strHintText;
        }

		public static string GetLineDisplayHint(PXGraph graph, string type, string srvOrdType, string refNbr, string lineRef)
		{
			string strHintText = string.Empty;
			if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(srvOrdType) || string.IsNullOrEmpty(refNbr))
				return strHintText;

			if (type == ListField_PostDoc_EntityType.APPOINTMENT)
			{
				strHintText = TX.PostDoc_EntityType.APPOINTMENT + ": ";
			}else
			{
				strHintText = TX.PostDoc_EntityType.SERVICE_ORDER + ": ";
			}

			strHintText += srvOrdType + "-" + refNbr;

			if (lineRef != null)
			{
				strHintText += " " + TX.FSAppointmentLogColumns.DetLineRef + ": " + lineRef;
			}

			return strHintText;
		}
	}

    public static class StringExtensionMethods
    {
        public static string EnsureEndsWithDot(this string str)
        {
            if (str == null) return string.Empty;

            str = str.Trim();

            if (str == string.Empty) return str;

            if (!str.EndsWith(".")) return str + ".";

            return str;
        }
    }
}
