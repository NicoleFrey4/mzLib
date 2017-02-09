﻿using Chemistry;
using Proteomics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UsefulProteomicsDatabases
{
    public static class PtmListLoader
    {

        #region Public Methods

        public static IEnumerable<Modification> ReadMods(string ptmListLocation)
        {
            using (StreamReader uniprot_mods = new StreamReader(ptmListLocation))
            {
                // UniProt fields
                string uniprotID = null;
                Tuple<string, string> uniprotAC = null;
                string uniprotFT = null;
                IEnumerable<string> uniprotTG = null;
                string uniprotPP = null;
                ChemicalFormula uniprotCF = null;
                double? uniprotMM = null;
                var uniprotDR = new Dictionary<string, HashSet<string>>();

                // Custom fields
                IEnumerable<double> neutralLosses = null;
                IEnumerable<double> massesObserved = null;
                IEnumerable<double> diagnosticIons = null;

                while (uniprot_mods.Peek() != -1)
                {
                    string line = uniprot_mods.ReadLine();
                    if (line.Length >= 2)
                    {
                        switch (line.Substring(0, 2))
                        {
                            case "ID":
                                uniprotID = line.Substring(5);
                                break;

                            case "AC":
                                uniprotAC = new Tuple<string, string>("uniprot", line.Substring(5));
                                break;

                            case "FT": // MOD_RES CROSSLNK LIPID
                                uniprotFT = line.Substring(5);
                                break;

                            case "TG": // Which amino acid(s) or motifs is the modification on
                                uniprotTG = new HashSet<string>(line.Substring(5).TrimEnd('.').Split(new string[] { " or " }, StringSplitOptions.None));
                                break;

                            case "PP": // Terminus localization
                                uniprotPP = line.Substring(5);
                                break;

                            case "CF": // Correction formula
                                uniprotCF = new ChemicalFormula(line.Substring(5).Replace(" ", string.Empty));
                                break;

                            case "MM": // Monoisotopic mass difference. Might not precisely correspond to formula!
                                uniprotMM = double.Parse(line.Substring(5));
                                break;

                            case "DR": // External database links!
                                var splitString = line.Substring(5).TrimEnd('.').Split(new string[] { "; " }, StringSplitOptions.None);
                                HashSet<string> val;
                                if (uniprotDR.TryGetValue(splitString[0], out val))
                                    val.Add(splitString[1]);
                                else
                                    uniprotDR.Add(splitString[0], new HashSet<string> { splitString[1] });
                                break;

                            // NOW CUSTOM FIELDS:

                            case "NL": // Netural Losses. If field doesn't exist, single equal to 0
                                neutralLosses = new HashSet<double>(line.Substring(5).Split(new string[] { " or " }, StringSplitOptions.None).Select(b => double.Parse(b)));
                                break;

                            case "OM": // What masses are seen in histogram. If field doesn't exist, single equal to MM
                                massesObserved = new HashSet<double>(line.Substring(5).Split(new string[] { " or " }, StringSplitOptions.None).Select(b => double.Parse(b)));
                                break;

                            case "DI": // Masses of diagnostic ions
                                var nice = line.Substring(5).Split(new string[] { " or " }, StringSplitOptions.None);
                                if (!string.IsNullOrEmpty(nice[0]))
                                    diagnosticIons = new HashSet<double>(nice.Select(b => double.Parse(b)));
                                else
                                    diagnosticIons = null;
                                break;

                            case "//":
                                // Only mod_res, not intrachain.
                                if ((uniprotFT == null || !uniprotFT.Equals("CROSSLNK")) && uniprotPP != null && uniprotTG != null && uniprotID != null)
                                {
                                    foreach (var singleTarget in uniprotTG)
                                    {
                                        // Add the modification!
                                        if (!uniprotMM.HasValue)
                                        {
                                            // Return modification
                                            yield return new Modification(uniprotID, uniprotAC, singleTarget, uniprotPP, uniprotDR, Path.GetFileNameWithoutExtension(ptmListLocation));
                                        }
                                        else
                                        {
                                            if (neutralLosses == null)
                                                neutralLosses = new HashSet<double> { 0 };
                                            foreach (var neutralLoss in neutralLosses)
                                            {
                                                if (uniprotCF == null)
                                                {
                                                    // Return modification with mass
                                                    yield return new ModificationWithMass(uniprotID, uniprotAC, singleTarget, uniprotPP, uniprotMM.Value, uniprotDR,
                                                        neutralLoss,
                                                        massesObserved == null ? new HashSet<double> { uniprotMM.Value } : massesObserved,
                                                        diagnosticIons,
                                                        Path.GetFileNameWithoutExtension(ptmListLocation));
                                                }
                                                else
                                                {
                                                    // Return modification with complete information!
                                                    yield return new ModificationWithMassAndCf(uniprotID, uniprotAC, singleTarget, uniprotPP, uniprotCF, uniprotMM.Value, uniprotDR,
                                                        neutralLoss,
                                                        massesObserved == null ? new HashSet<double> { uniprotMM.Value } : massesObserved,
                                                        diagnosticIons,
                                                        Path.GetFileNameWithoutExtension(ptmListLocation));
                                                }
                                            }
                                        }
                                    }
                                }

                                uniprotID = null;
                                uniprotAC = null;
                                uniprotFT = null;
                                uniprotTG = null;
                                uniprotPP = null;
                                uniprotCF = null;
                                uniprotMM = null;
                                uniprotDR = new Dictionary<string, HashSet<string>>();

                                // Custom fields
                                neutralLosses = null;
                                massesObserved = null;
                                diagnosticIons = null;

                                break;
                        }
                    }
                }
            }
        }

        #endregion Public Methods

    }
}