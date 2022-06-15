using System;
using System.Linq;

namespace PX.Objects.Localizations.CA.AP
{
    /// <summary>
    /// Code provided by the AcomabaX dev team, it converts amounts to letters in french only
    /// 
    /// Function and variable names are in French (at the time of this writing we do not want to 
    /// invest time in translating names).
    /// 
    /// </summary>
    public static class AmountToLetterHelper
    {
        //FRENCH LETTER CONVERSION

        private static readonly string[] FR1 = { string.Empty, "UN", "DEUX", "TROIS", "QUATRE", "CINQ", "SIX", "SEPT", "HUIT", "NEUF" };
        private static readonly string[] FR2 = { string.Empty, "DIX", "ONZE", "DOUZE", "TREIZE", "QUATORZE", "QUINZE", "SEIZE", "DIX", "DIX", "DIX" };
        private static readonly string[] FR3 = { string.Empty, "UN", "VINGT", "TRENTE", "QUARANTE", "CINQUANTE", "SOIXANTE" };

        private const string NEGATION = "-";
        private const string MULTIPLE = "S";
        private const string ESPACE = " ";

        private static string Unite(decimal amount, string carVar)
        {
            Int64 entier = (Int64)amount;
            string stringNumber = entier.ToString();

            int count = stringNumber.Length;
            int unitePos = count - 1;
            int dizainePos = count - 2;
            
            string result = FR1[int.Parse(stringNumber[unitePos].ToString())];

            if (count > 1 &&
                stringNumber[unitePos] == '1' &&
                new[] { 2, 3, 4, 5, 6 }.Contains((int)char.GetNumericValue(stringNumber, dizainePos)))
            {
                result = " ET " + result;
            }

            return result;
        }

        private static string Dixaine(decimal amount, string carVar)
        {
            string signe = NEGATION;
            string result = string.Empty;

            Int64 entier = (Int64)amount;
            string stringNumber = entier.ToString();

            int count = stringNumber.Length;
            int unitePos = count - 1;
            int dizainePos = count - 2;

            int uniteValue = (int)char.GetNumericValue(stringNumber[unitePos]);

            if (uniteValue == 0 || uniteValue == 1)
            {
                signe = string.Empty;
            }

            int dizaineValue = (int)char.GetNumericValue(stringNumber[dizainePos]);

            switch (dizaineValue)
            {
                case 1:
                    if (uniteValue == 7 || uniteValue == 8 || uniteValue == 9)
                    {
                        result = string.Format("{0}{1}", "DIX-", carVar);
                    }
                    else
                    {
                        result = FR2[uniteValue + 1];
                    }
                    break;

                case 7:
                case 9:
                    if (dizaineValue == 7)
                    {
                        result = "SOIXANTE";
                    }
                    else
                    {
                        result = "QUATRE-VINGT";
                    }

                    switch (uniteValue)
                    {
                        case 0:
                            result = string.Format("{0}{1}", result, "-DIX");
                            break;

                        case 1:
                            if (dizaineValue == 7)
                            {
                                result = string.Format("{0}{1}", result, " ET ONZE");
                            }
                            else
                            {
                                result = string.Format("{0}{1}", result, "-ONZE");
                            }
                            break;

                        default:
                            if ((dizaineValue == 7 || dizaineValue == 9) &&
                                 !new[] { 2, 3, 4, 5, 6 }.Contains(uniteValue))
                            {
                                result = string.Format("{0}{1}{2}", result, "-DIX-", FR1[uniteValue]);
                            }
                            else
                            {
                                result = string.Format("{0}{1}{2}", result, NEGATION, FR2[uniteValue + 1]);
                            }
                            break;
                    }
                    break;

                case 8:
                    if (string.IsNullOrEmpty(carVar))
                    {
                        result = "QUATRE-VINGTS";
                    }
                    else
                    {
                        result = string.Format("{0}{1}{2}", result, "QUATRE-VINGT-", carVar);
                    }
                    break;

                default:
                    if (dizaineValue != 0)
                    {
                        result = string.Format("{0}{1}{2}", FR3[dizaineValue], signe, carVar);
                    }
                    else
                    {
                        result = carVar;
                    }
                    break;
            }

            return result;
        }

        private static string Centaine(decimal amount, string carVar)
        {
            string result;

            Int64 entier = (Int64)amount;
            string stringNumber = entier.ToString();
            int count = stringNumber.Length;

            int number = (int)char.GetNumericValue(stringNumber, count - 3);

            if (number != 0)
            {
                var signe = (string.IsNullOrEmpty(carVar) && number > 1) ? MULTIPLE : ESPACE;

                if (number == 1 && string.IsNullOrEmpty(carVar))
                {
                    signe = string.Empty;
                }

                result = (number == 1) ? ("CENT" + signe + carVar) : (FR1[number] + " CENT" + signe + carVar);
            }
            else
            {
                result = carVar;
            }

            return result;
        }

        private static string Millieme(decimal amount, string carVar)
        {
            string result;

            Int64 entier = (Int64)amount;
            string stringNumber = entier.ToString();

            int count = stringNumber.Length;
            int millierPos = count - 4;
            int dizaineMillierPos = count - 5;

            if (string.IsNullOrEmpty(carVar))
            {
                result = string.Format("{0}", "MILLE");
            }
            else
            {
                result = string.Format("{0} {1}", "MILLE", carVar);
            }

            int number = (int)char.GetNumericValue(stringNumber, millierPos);
            int previousNumber = count > 4 ? (int)char.GetNumericValue(stringNumber, dizaineMillierPos) : 0;

            if (count >= 4)
            {
                if ((((new[] { 7, 8, 9 }).Contains(number)) ||
                     (new[] { 0, 2, 3, 4, 5, 6, 8 }).Contains(previousNumber) ||
                     previousNumber == 0) && number != 1)
                {
                    result = FR1[(int)char.GetNumericValue(stringNumber[millierPos])] + ((previousNumber == 0 && number == 0) ? string.Empty : ESPACE) + result;
                }

                if ((new[] { 0, 1, 2, 3, 4, 5, 6 }).Contains(number) &&
                    previousNumber == 1)
                {
                    result = ESPACE + result;
                }

                if (count > 4 && number == 1 &&
                    (new[] { 0, 2, 3, 4, 5, 6, 8 }).Contains(previousNumber))
                {
                    result = "UN " + result;
                }

                if (number == 1 &&
                  (new[] { 2, 3, 4, 5, 6 }).Contains(previousNumber))
                {
                    result = " ET " + result;
                }
            }

            return result;
        }

        private static string DixMillieme(decimal amount, string carVar)
        {
            string signe;
            string result;

            Int64 entier = (Int64)amount;
            string stringNumber = entier.ToString();
            int count = stringNumber.Length;
            int millierPos = count - 4;
            int dizaineMillierPos = count - 5;


            int nextNumber = (int)char.GetNumericValue(stringNumber, millierPos);
            int number = (int)char.GetNumericValue(stringNumber, dizaineMillierPos);

            if (nextNumber == 0 || nextNumber == 1)
            {
                signe = string.Empty;
            }
            else
            {
                signe = NEGATION;
            }

            switch (number)
            {
                case 1:
                    result = (new[] { 7, 8, 9 }).Contains(nextNumber) ? "DIX-" + carVar : FR2[nextNumber + 1] + carVar;
                    break;

                case 7:
                case 9:
                    string tempString = number == 7 ? "SOIXANTE" : "QUATRE-VINGT";

                    switch (nextNumber)
                    {
                        case 0:
                            result = tempString + "-DIX " + carVar;
                            break;
                        case 1:
                            result = tempString + (number == 7 ? " ET ONZE " : "-ONZE ") + carVar;
                            break;
                        default:
                            result = (new[] { 7, 8, 9 }).Contains(nextNumber) ? tempString + "-DIX-" + carVar :
                                                                                    tempString + NEGATION + FR2[nextNumber + 1] + ESPACE + carVar;
                            break;
                    }
                    break;

                case 8:
                    result = nextNumber == 0 ? "QUATRE-VINGT" + carVar : "QUATRE-VINGT-" + carVar;
                    break;

                default:
                    if (number != 0)
                    {
                        result = FR3[number] + signe + carVar;
                    }
                    else
                    {
                        result = carVar;
                    }
                    break;
            }

            return result;
        }

        private static string CentMillieme(decimal amount, string carVar)
        {
            string signe = string.Empty;
            string result = string.Empty;
            string tempString = string.Empty;

            Int64 entier = (Int64)amount;
            string stringNumber = entier.ToString();
            int count = stringNumber.Length;
            int centaineMillierPos = count - 6;

            int number = (int)char.GetNumericValue(stringNumber, centaineMillierPos);

            if (string.IsNullOrWhiteSpace(carVar))
            {
                carVar = ESPACE + carVar;
            }

            if (number == 1)
            {
                result = "CENT " + carVar;
            }
            else if (number != 0)
            {
                result = FR1[number] + " CENT " + carVar;
            }

            return result;
        }

        /// <summary>
        /// Nombre the lettre.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="isEnglish">if set to <c>true</c> [is english].</param>
        /// <returns></returns>
        public static string NombreLettre(decimal number)
        {
            if (number >= 1000000m)
                return string.Format("{0:n2}", number);

            string entierResult = string.Empty;
            string decimalResult;
            int partieEntiere = (int)decimal.Truncate(number);
            int partieDecimale = (int)decimal.Truncate((number - partieEntiere) * 100);

            if (partieEntiere > 0)
                entierResult = Unite(partieEntiere, entierResult);
            if (partieEntiere >= 10)
                entierResult = Dixaine(partieEntiere, entierResult);
            if (partieEntiere >= 100)
                entierResult = Centaine(partieEntiere, entierResult);
            if (partieEntiere >= 1000)
                entierResult = Millieme(partieEntiere, entierResult);
            if (partieEntiere >= 10000)
                entierResult = DixMillieme(partieEntiere, entierResult);
            if (partieEntiere >= 100000)
                entierResult = CentMillieme(partieEntiere, entierResult);

            entierResult = string.IsNullOrEmpty(entierResult) ? "ZERO" : entierResult;

            if (partieDecimale == 0)
            {
                decimalResult = "00/100";
            }
            else
            {
                decimalResult = partieDecimale.ToString() + "/100";
            }

            return string.Format("{0} ET {1}", entierResult, decimalResult);
        }
    }
}
