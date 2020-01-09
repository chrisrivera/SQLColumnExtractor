using gudusoft.gsqlparser;
using System;
using System.Collections.Generic;
using System.Text;

namespace ColumnExtracter.Parser
{
    public static class DBParser
    {
        static List<string> tablelist, columnlist, databaselist, schemalist, functionlist, triggerlist, sequencelist;
        static StringBuilder tcList;
        static Boolean showLocation = false, showEffect = false;

        public static ParseResult ExtractColumns(string sqlStr)
        {
            TGSqlParser sqlparser = new TGSqlParser(TDbVendor.DbVOracle);
            sqlparser.SqlText.Text = sqlStr;
            int ret = sqlparser.Parse();
            if (ret != 0)
            {
                throw new Exception("Unable to parse sql.");
            }

            tablelist = new List<string>();
            columnlist = new List<string>();
            databaselist = new List<string>();
            schemalist = new List<string>();
            functionlist = new List<string>();
            triggerlist = new List<string>();
            sequencelist = new List<string>();
            tcList = new StringBuilder();

            for (int i = 0; i < sqlparser.SqlStatements.Count(); i++)
            {
                TCustomSqlStatement sql = sqlparser.SqlStatements[i];
                AnalyzeStmt(sql, 0);
            }
            
            SortAndRemoveDup(tablelist);
            SortAndRemoveDup(columnlist);
            SortAndRemoveDup(databaselist);
            SortAndRemoveDup(schemalist);
            SortAndRemoveDup(functionlist);
            SortAndRemoveDup(triggerlist);
            SortAndRemoveDup(sequencelist);

            ParseResult result = new ParseResult()
            {
                Columnlist = columnlist,
                Databaselist = databaselist,
                Functionlist = functionlist,
                Schemalist = schemalist,
                Sequencelist = schemalist,
                Tablelist = tablelist,
                Triggerlist = triggerlist,
                Structure = tcList.ToString()
            };

            return result;
        }

        static void AnalyzeStmt(TCustomSqlStatement stmt, int pNest)
        {
            tcList.AppendLine("");
            tcList.AppendLine(string.Format("{0}{1}", " ".PadLeft(pNest, ' '), stmt.SqlStatementType));

            string tn = "", tneffect = "";
            for (int k = 0; k < stmt.Tables.Count(); k++)
            {
                if (stmt.Tables[k].TableType == TLzTableType.lttSubquery)
                {
                    tn = "(subquery, alias:" + stmt.Tables[k].TableAlias + ")";
                }
                else
                {
                    tn = stmt.Tables[k].TableFullname;
                    if (stmt.Tables[k].isLinkTable)
                    {
                        tn = tn + "(" + stmt.Tables[k].linkedTable.TableFullname + ")";
                    }
                    else if (stmt.Tables[k].isCTE)
                    {
                        tn = tn + "(CTE)";
                    }

                }
                if (!((stmt.Tables[k].TableType == TLzTableType.lttSubquery) || (stmt.Tables[k].isCTE)))
                {
                    tablelist.Add(tn);
                }

                if (showEffect)
                {
                    tneffect = string.Format("{0}({1})", tn, stmt.Tables[k].effectType);
                }
                else
                {
                    tneffect = string.Format("{0}", tn);
                }
                tcList.AppendLine(string.Format("{0}{1}", " ".PadLeft(pNest + 1, ' '), tneffect));


                for (int m = 0; m < stmt.Tables[k].linkedColumns.Count(); m++)
                {
                    String columnInfo = "";
                    if (showLocation)
                    {
                        columnInfo = string.Format("{0}({1})", stmt.Tables[k].linkedColumns[m].fieldAttrName, stmt.Tables[k].linkedColumns[m].Location);
                    }
                    else
                    {
                        columnInfo = string.Format("{0}", stmt.Tables[k].linkedColumns[m].fieldAttrName);
                    }
                    tcList.AppendLine(string.Format("{0}{1}", " ".PadLeft(pNest + 2, ' '), columnInfo));

                    if (!((stmt.Tables[k].TableType == TLzTableType.lttSubquery) || (stmt.Tables[k].isCTE)))
                    {
                        if (stmt.Tables[k].isLinkTable)
                        { //mssql, deleted, inserted table
                            columnlist.Add(stmt.Tables[k].linkedTable.TableFullname + '.' + stmt.Tables[k].linkedColumns[m].fieldAttrName);
                        }
                        else
                            columnlist.Add(tn + '.' + stmt.Tables[k].linkedColumns[m].fieldAttrName);
                    }
                }
            }

            if (stmt.orphanColumns.Count() > 0)
            {
                tcList.AppendLine(string.Format("{0}{1}", " ".PadLeft(pNest + 1, ' '), " orphan columns:"));
                for (int k = 0; k < stmt.orphanColumns.Count(); k++)
                {
                    tcList.AppendLine(string.Format("{0}{1}", " ".PadLeft(pNest + 2, ' '), stmt.orphanColumns[k].AsText));
                    columnlist.Add("missing." + stmt.orphanColumns[k].AsText);
                }
            }

            if (stmt.DatabaseTokens.Count() > 0)
            {
                for (int k = 0; k < stmt.DatabaseTokens.Count(); k++)
                {
                    databaselist.Add(stmt.DatabaseTokens[k].AsText);
                }
            }

            if (stmt.SchemaTokens.Count() > 0)
            {
                for (int k = 0; k < stmt.SchemaTokens.Count(); k++)
                {
                    schemalist.Add(stmt.SchemaTokens[k].AsText);
                }
            }

            if (stmt.FunctionTokens.Count() > 0)
            {
                for (int k = 0; k < stmt.FunctionTokens.Count(); k++)
                {
                    if (stmt.FunctionTokens[k].ParentToken != null)
                    {
                        functionlist.Add(stmt.FunctionTokens[k].ParentToken.AsText + "." + stmt.FunctionTokens[k].AsText);
                    }
                    else
                    {
                        functionlist.Add(stmt.FunctionTokens[k].AsText);
                    }
                }
            }

            if (stmt.TriggerTokens.Count() > 0)
            {
                for (int k = 0; k < stmt.TriggerTokens.Count(); k++)
                {
                    if (stmt.TriggerTokens[k].ParentToken != null)
                    {
                        triggerlist.Add(stmt.TriggerTokens[k].ParentToken.AsText + "." + stmt.TriggerTokens[k].AsText);
                    }
                    else
                    {
                        triggerlist.Add(stmt.TriggerTokens[k].AsText);
                    }
                }
            }

            if (stmt.SequenceTokens.Count() > 0)
            {
                for (int k = 0; k < stmt.SequenceTokens.Count(); k++)
                {
                    sequencelist.Add(stmt.SequenceTokens[k].AsText);
                }
            }


            for (int j = 0; j < stmt.ChildNodes.Count(); j++)
            {
                if (stmt.ChildNodes[j] is TCustomSqlStatement)
                {
                    AnalyzeStmt(stmt.ChildNodes[j] as TCustomSqlStatement, pNest + 1);
                }
            }
        }

        static void SortAndRemoveDup(List<string> pList)
        {
            pList.Sort();
            Int32 index = 0;
            while (index < pList.Count - 1)
            {
                if (pList[index] == pList[index + 1])
                    pList.RemoveAt(index);
                else
                    index++;
            }
        }

    }
}
