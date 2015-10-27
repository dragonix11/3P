﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _3PA.Lib;
using _3PA.MainFeatures.DockableExplorer;
using _3PA.MainFeatures.Parser;

namespace _3PA.MainFeatures.AutoCompletion {

    /// <summary>
    /// This class sustains the autocompletion list AND the code explorer list
    /// by visiting the parser and creating new completionData
    /// </summary>
    class ParserVisitor : IParserVisitor {

        #region Fields

        private const string BlockTooLongString = "Too long!";

        /// <summary>
        /// Are we currently visiting the current file opened in npp or
        /// is it a include?
        /// </summary>
        private bool _isBaseFile;

        /// <summary>
        /// Stores the file name of the file currently visited/parsed
        /// </summary>
        private string _currentParsedFile;

        /// <summary>
        /// this dictionnary is used to reference the procedures defined
        /// in the program we are parsing, dictionnary is faster that list when it comes to
        /// test if a procedure/function exists in the program
        /// </summary>
        public Dictionary<string, bool> DefinedProcedures = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Line info from the parser
        /// </summary>
        private Dictionary<int, LineInfo> _lineInfo;

        /// <summary>
        /// contains the list of items that depend on the current file, that list
        /// is updated by the parser's visitor class
        /// </summary>
        public List<CompletionData> ParsedItemsList = new List<CompletionData>();

        /// <summary>
        /// Contains the list of explorer items for the current file, updated by the parser's visitor class
        /// </summary>
        public List<CodeExplorerItem> ParsedExplorerItemsList = new List<CodeExplorerItem>();

        #endregion

        #region constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="isBaseFile"></param>
        /// <param name="currentParsedFile"></param>
        /// <param name="lineInfo"></param>
        public ParserVisitor(bool isBaseFile, string currentParsedFile, Dictionary<int, LineInfo> lineInfo) {
            _isBaseFile = isBaseFile;
            _currentParsedFile = currentParsedFile;
            _lineInfo = lineInfo;
        }

        #endregion

        #region visit implementation

        /// <summary>
        /// Run statement,
        /// a second pass will be done after the visit is over to determine if a run is
        /// internal or external (calling internal proc or programs)
        /// </summary>
        /// <param name="pars"></param>
        public void Visit(ParsedRun pars) {
            // to code explorer
            ParsedExplorerItemsList.Add(new CodeExplorerItem() {
                DisplayText = pars.Name,
                Branch = CodeExplorerBranch.Run,
                IconType = CodeExplorerIconType.RunExternal,
                IsNotBlock = true,
                Flag = AddExternalFlag(pars.IsEvaluateValue ? CodeExplorerFlag.Uncertain : 0),
                DocumentOwner = pars.FilePath,
                GoToLine = pars.Line,
                GoToColumn = pars.Column,
                SubString = SetExternalInclude(null)
            });
        }

        /// <summary>
        /// Dynamic-function
        /// </summary>
        /// <param name="pars"></param>
        public void Visit(ParsedFunctionCall pars) {
            // To code explorer
            ParsedExplorerItemsList.Add(new CodeExplorerItem() {
                DisplayText = pars.Name,
                Branch = CodeExplorerBranch.DynamicFunctionCall,
                IconType = pars.ExternalCall ? CodeExplorerIconType.FunctionCallExternal : CodeExplorerIconType.FunctionCallInternal,
                IsNotBlock = true,
                Flag = AddExternalFlag((CodeExplorerFlag)0),
                DocumentOwner = pars.FilePath,
                GoToLine = pars.Line,
                GoToColumn = pars.Column,
                SubString = SetExternalInclude(null)
            });
        }

        /// <summary>
        /// Tables used in the program
        /// </summary>
        /// <param name="pars"></param>
        public void Visit(ParsedFoundTableUse pars) {
            bool missingDbName = pars.Name.IndexOf('.') < 0;
            var name = pars.Name.Split('.');

            // to code explorer
            ParsedExplorerItemsList.Add(new CodeExplorerItem() {
                DisplayText = missingDbName ? pars.Name : name[1],
                Branch = CodeExplorerBranch.TableUsed,
                IconType = CodeExplorerIconType.Table,
                Flag = AddExternalFlag(missingDbName ? CodeExplorerFlag.MissingDbName : 0),
                IsNotBlock = true,
                DocumentOwner = pars.FilePath,
                GoToLine = pars.Line,
                GoToColumn = pars.Column,
                SubString = SetExternalInclude(null)
            });
        }

        /// <summary>
        /// Include files
        /// </summary>
        /// <param name="pars"></param>
        public void Visit(ParsedIncludeFile pars) {

            // try to find the file in the propath
            var fullFilePath = ProgressEnv.FindFileInPropath(pars.Name);

            // To code explorer
            ParsedExplorerItemsList.Add(new CodeExplorerItem() {
                DisplayText = pars.Name,
                Branch = CodeExplorerBranch.Include,
                IsNotBlock = true,
                Flag = AddExternalFlag(string.IsNullOrEmpty(fullFilePath) ? CodeExplorerFlag.NotFound : 0),
                DocumentOwner = pars.FilePath,
                GoToLine = pars.Line,
                GoToColumn = pars.Column,
                SubString = SetExternalInclude(null)
            });

            // Parse the include file
            if (string.IsNullOrEmpty(fullFilePath)) return;

            ParserVisitor parserVisitor;

            // did we already parsed this file?
            if (ParserHandler.SavedParserVisitors.ContainsKey(fullFilePath)) {
                parserVisitor = ParserHandler.SavedParserVisitors[fullFilePath];
            } else {
                // Parse it
                var ablParser = new Parser.Parser(File.ReadAllText(fullFilePath, TextEncodingDetect.GetFileEncoding(fullFilePath)), fullFilePath, pars.OwnerName, DataBase.GetTablesDictionary());

                parserVisitor = new ParserVisitor(false, Path.GetFileName(fullFilePath), ablParser.GetLineInfo);
                ablParser.Accept(parserVisitor);

                // save it for future uses
                ParserHandler.SavedParserVisitors.Add(fullFilePath, parserVisitor);
            }

            // add info from the parser
            ParsedItemsList.AddRange(parserVisitor.ParsedItemsList.ToList());
            if (Config.Instance.CodeExplorerDisplayExternalItems)
                ParsedExplorerItemsList.AddRange(parserVisitor.ParsedExplorerItemsList.ToList());

            // fill the defined procedures dictionnary
            foreach (var definedProcedure in parserVisitor.DefinedProcedures.Where(definedProcedure => !DefinedProcedures.ContainsKey(definedProcedure.Key))) {
                DefinedProcedures.Add(definedProcedure.Key, definedProcedure.Value);
            }
        }



        /// <summary>
        /// Main block, definitions block...
        /// </summary>
        /// <param name="pars"></param>
        public void Visit(ParsedBlock pars) {
            // to code explorer
            ParsedExplorerItemsList.Add(new CodeExplorerItem {
                DisplayText = pars.Name,
                Branch = pars.Branch,
                IconType = pars.IconIconType,
                Level = pars.IsRoot ? 0 : 1,
                Flag = AddExternalFlag((CodeExplorerFlag)0),
                DocumentOwner = pars.FilePath,
                GoToLine = pars.Line,
                GoToColumn = pars.Column,
                SubString = SetExternalInclude(null)
            });
        }

        /// <summary>
        /// ON events
        /// </summary>
        /// <param name="pars"></param>
        public void Visit(ParsedOnEvent pars) {
            // check lenght of block
            pars.TooLongForAppbuilder = HasTooMuchChar(pars.Line, pars.EndLine);

            // To code explorer
            ParsedExplorerItemsList.Add(new CodeExplorerItem() {
                DisplayText = string.Join(" ", pars.On.ToUpper(), pars.Name),
                Branch = CodeExplorerBranch.OnEvent,
                Flag = AddExternalFlag(pars.TooLongForAppbuilder ? CodeExplorerFlag.IsTooLong : 0),
                DocumentOwner = pars.FilePath,
                GoToLine = pars.Line,
                GoToColumn = pars.Column,
                SubString = SetExternalInclude(pars.TooLongForAppbuilder ? BlockTooLongString + " (+" + NbExtraCharBetweenLines(pars.Line, pars.EndLine) + ")" : null)
            });
        }

        /// <summary>
        /// Functions
        /// </summary>
        /// <param name="pars"></param>
        public void Visit(ParsedFunction pars) {
            // check lenght of block
            pars.TooLongForAppbuilder = HasTooMuchChar(pars.Line, pars.EndLine);

            // to code explorer
            ParsedExplorerItemsList.Add(new CodeExplorerItem() {
                DisplayText = pars.Name,
                Branch = CodeExplorerBranch.Function,
                Flag = AddExternalFlag(pars.TooLongForAppbuilder ? CodeExplorerFlag.IsTooLong : 0),
                DocumentOwner = pars.FilePath,
                GoToLine = pars.Line,
                GoToColumn = pars.Column,
                SubString = SetExternalInclude(pars.TooLongForAppbuilder ? BlockTooLongString + " (+" + NbExtraCharBetweenLines(pars.Line, pars.EndLine) + ")" : null)
            });

            // to completion data
            pars.ReturnType = ParserHandler.ConvertStringToParsedPrimitiveType(pars.ParsedReturnType, false);
            ParsedItemsList.Add(new CompletionData() {
                DisplayText = pars.Name,
                Type = CompletionType.Function,
                SubString = pars.ReturnType.ToString(),
                Flag = AddExternalFlag((pars.IsPrivate ? ParseFlag.Private : 0) | (pars.IsExtended ? ParseFlag.Extent : 0)),
                Ranking = ParserHandler.FindRankingOfParsedItem(pars.Name),
                ParsedItem = pars,
                FromParser = true
            });
        }

        /// <summary>
        /// Procedures
        /// </summary>
        /// <param name="pars"></param>
        public void Visit(ParsedProcedure pars) {
            // check lenght of block
            pars.TooLongForAppbuilder = HasTooMuchChar(pars.Line, pars.EndLine);

            // fill dictionnary containing the name of all procedures defined
            if (!DefinedProcedures.ContainsKey(pars.Name))
                DefinedProcedures.Add(pars.Name, false);

            // to code explorer
            ParsedExplorerItemsList.Add(new CodeExplorerItem() {
                DisplayText = pars.Name,
                Branch = CodeExplorerBranch.Procedure,
                Flag = AddExternalFlag(pars.TooLongForAppbuilder ? CodeExplorerFlag.IsTooLong : 0),
                DocumentOwner = pars.FilePath,
                GoToLine = pars.Line,
                GoToColumn = pars.Column,
                SubString = SetExternalInclude(pars.TooLongForAppbuilder ? BlockTooLongString + " (+" + NbExtraCharBetweenLines(pars.Line, pars.EndLine) + ")" : null)
            });

            // to completion data
            ParsedItemsList.Add(new CompletionData() {
                DisplayText = pars.Name,
                Type = CompletionType.Procedure,
                SubString = !_isBaseFile ? _currentParsedFile : string.Empty,
                Flag = AddExternalFlag(pars.IsExternal ? ParseFlag.ExternalProc : 0),
                Ranking = ParserHandler.FindRankingOfParsedItem(pars.Name),
                ParsedItem = pars,
                FromParser = true
            });
        }




        /// <summary>
        /// Preprocessed variables
        /// </summary>
        /// <param name="pars"></param>
        public void Visit(ParsedPreProc pars) {

            // to completion data
            ParsedItemsList.Add(new CompletionData() {
                DisplayText = "&" + pars.Name,
                Type = CompletionType.Preprocessed,
                SubString = !_isBaseFile ? _currentParsedFile : string.Empty,
                Flag = AddExternalFlag(pars.Scope == ParsedScope.File ? ParseFlag.FileScope : ParseFlag.LocalScope),
                Ranking = ParserHandler.FindRankingOfParsedItem(pars.Name),
                ParsedItem = pars,
                FromParser = true
            });
        }

        /// <summary>
        /// Labels
        /// </summary>
        /// <param name="pars"></param>
        public void Visit(ParsedLabel pars) {

            if (!_isBaseFile) return;

            // find the end line of the labelled block
            var line = pars.Line + 1;
            var depth = (_lineInfo.ContainsKey(pars.Line)) ? _lineInfo[pars.Line].BlockDepth : 0;
            bool wentIntoBlock = false;
            while (_lineInfo.ContainsKey(line)) {
                if (!wentIntoBlock && _lineInfo[line].BlockDepth > depth) {
                    wentIntoBlock = true;
                    depth = _lineInfo[line].BlockDepth;
                } else if (wentIntoBlock && _lineInfo[line].BlockDepth < depth)
                    break;
                line++;
            }
            pars.UndefinedLine = line;

            // to completion data
            ParsedItemsList.Add(new CompletionData() {
                DisplayText = pars.Name,
                Type = CompletionType.Label,
                SubString = !_isBaseFile ? _currentParsedFile : string.Empty,
                Flag = 0,
                Ranking = ParserHandler.FindRankingOfParsedItem(pars.Name),
                ParsedItem = pars,
                FromParser = true
            });
        }

        /// <summary>
        /// Defined variables
        /// </summary>
        /// <param name="pars"></param>
        public void Visit(ParsedDefine pars) {
            // set flags
            var flag = pars.Scope == ParsedScope.File ? ParseFlag.FileScope : ParseFlag.LocalScope;
            if (pars.Type == ParseDefineType.Parameter) flag = flag | ParseFlag.Parameter;
            if (pars.IsExtended) flag = flag | ParseFlag.Extent;

            // find primitive type
            var hasPrimitive = !string.IsNullOrEmpty(pars.TempPrimitiveType);
            if (hasPrimitive)
                pars.PrimitiveType = ParserHandler.ConvertStringToParsedPrimitiveType(pars.TempPrimitiveType, pars.AsLike == ParsedAsLike.Like);

            // which completionData type is it?
            CompletionType type;
            string subString;
            // special case for buffers, they go into the temptable or table section
            if (pars.PrimitiveType == ParsedPrimitiveType.Buffer) {
                flag = flag | ParseFlag.Buffer;
                subString = "?";
                type = CompletionType.TempTable;

                // find the table or temp table that the buffer is FOR
                var foundTable = ParserHandler.FindAnyTableByName(pars.BufferFor);
                if (foundTable != null) {
                    subString = foundTable.Name.AutoCaseToUserLiking();
                    type = foundTable.IsTempTable ? CompletionType.TempTable : CompletionType.Table;

                    // To code explorer, list buffers and associated tables
                    ParsedExplorerItemsList.Add(new CodeExplorerItem() {
                        DisplayText = foundTable.Name,
                        Branch = CodeExplorerBranch.TableUsed,
                        IconType = CodeExplorerIconType.TempTable,
                        Flag = AddExternalFlag(pars.BufferFor.IndexOf('.') >= 0 ? 0 : CodeExplorerFlag.MissingDbName),
                        IsNotBlock = true,
                        DocumentOwner = pars.FilePath,
                        GoToLine = pars.Line,
                        GoToColumn = pars.Column,
                        SubString = SetExternalInclude(null)
                    });
                }

            } else {
                // match type for everything else
                subString = hasPrimitive ? pars.PrimitiveType.ToString() : pars.Type.ToString();
                switch (pars.Type) {
                    case ParseDefineType.Parameter:
                        type = CompletionType.VariablePrimitive;

                        // To code explorer, program parameters
                        if (_isBaseFile && pars.Scope == ParsedScope.File)
                            ParsedExplorerItemsList.Add(new CodeExplorerItem() {
                                DisplayText = pars.Name,
                                Branch = CodeExplorerBranch.ProgramParameter,
                                IconType = CodeExplorerIconType.Parameter,
                                IsNotBlock = true,
                                Flag = AddExternalFlag((CodeExplorerFlag)0),
                                DocumentOwner = pars.FilePath,
                                GoToLine = pars.Line,
                                GoToColumn = pars.Column,
                                SubString = SetExternalInclude(subString)
                            });
                        break;
                    case ParseDefineType.Variable:
                        if (!string.IsNullOrEmpty(pars.ViewAs))
                            type = CompletionType.Widget;
                        else if ((int) pars.PrimitiveType < 30)
                            type = CompletionType.VariablePrimitive;
                        else
                            type = CompletionType.VariableComplex;
                        break;
                    case ParseDefineType.Button:
                    case ParseDefineType.Browse:
                    case ParseDefineType.Frame:
                    case ParseDefineType.Image:
                    case ParseDefineType.SubMenu:
                    case ParseDefineType.Menu:
                    case ParseDefineType.Rectangle:
                        type = CompletionType.Widget;
                        break;
                    default:
                        type = CompletionType.VariableComplex;
                        break;
                }
            }

            // To explorer code for browse
            if (pars.Type == ParseDefineType.Browse) {
                ParsedExplorerItemsList.Add(new CodeExplorerItem() {
                    DisplayText = pars.Name,
                    Branch = CodeExplorerBranch.Browse,
                    Flag = AddExternalFlag((CodeExplorerFlag)0),
                    DocumentOwner = pars.FilePath,
                    GoToLine = pars.Line,
                    GoToColumn = pars.Column,
                    SubString = SetExternalInclude(null)
                });
            }

            // to completion data
            ParsedItemsList.Add(new CompletionData() {
                DisplayText = pars.Name,
                Type = type,
                SubString = subString,
                Flag = AddExternalFlag(SetFlags(flag, pars.LcFlagString)),
                Ranking = ParserHandler.FindRankingOfParsedItem(pars.Name),
                ParsedItem = pars,
                FromParser = true
            });
        }

        /// <summary>
        /// Defined Temptables
        /// </summary>
        /// <param name="pars"></param>
        public void Visit(ParsedTable pars) {
            string subStr = "";

            // find all primitive types
            foreach (var parsedField in pars.Fields)
                parsedField.Type = ParserHandler.ConvertStringToParsedPrimitiveType(parsedField.TempType, parsedField.AsLike == ParsedAsLike.Like);

            // temp table is LIKE another table? copy fields
            if (!string.IsNullOrEmpty(pars.LcLikeTable)) {
                var foundTable = ParserHandler.FindAnyTableByName(pars.LcLikeTable);
                if (foundTable != null) {
                    // handles the use-index, for now only add the isPrimary flag to the field...
                    if (!string.IsNullOrEmpty(pars.UseIndex)) {
                        // add the fields of the found table (minus the primary information)
                        subStr = @"Like " + foundTable.Name;
                        foreach (var field in foundTable.Fields) {
                            pars.Fields.Add(new ParsedField(field.Name, "", field.Format, field.Order, field.Flag.HasFlag(ParsedFieldFlag.Mandatory) ? ParsedFieldFlag.Mandatory : 0, field.InitialValue, field.Description, field.AsLike) {
                                Type = field.Type
                            });
                        }
                        foreach (var index in pars.UseIndex.Split(',')) {
                            // we found a primary index
                            var foundIndex = foundTable.Indexes.Find(index2 => index2.Name.EqualsCi(index.Replace("!", "")));
                            // if the index is a primary
                            if (foundIndex != null && (foundIndex.Flag.HasFlag(ParsedIndexFlag.Primary) || index.ContainsFast("!")))
                                foreach (var fieldName in foundIndex.FieldsList) {
                                    // then the field is primary
                                    var foundfield = pars.Fields.Find(field => field.Name.EqualsCi(fieldName.Replace("+", "").Replace("-", "")));
                                    if (foundfield != null) foundfield.Flag = foundfield.Flag | ParsedFieldFlag.Primary;
                                }
                        }
                    } else {
                        // if there is no "use index", the tt uses the same index as the original table
                        pars.Fields = foundTable.Fields.ToList();
                    }
                }
            }

            ParsedItemsList.Add(new CompletionData() {
                DisplayText = pars.Name,
                Type = CompletionType.TempTable,
                SubString = subStr,
                Flag = AddExternalFlag(SetFlags(0, pars.LcFlagString)),
                Ranking = ParserHandler.FindRankingOfParsedItem(pars.Name),
                ParsedItem = pars,
                FromParser = true
            });
        }

        #endregion

        #region helper

        /// <summary>
        /// Adds the "external" flag if needed
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        private ParseFlag AddExternalFlag(ParseFlag flag) {
            if (_isBaseFile) return flag;
            return flag | ParseFlag.External;
        }

        private CodeExplorerFlag AddExternalFlag(CodeExplorerFlag flag) {
            if (_isBaseFile) return flag;
            return flag | CodeExplorerFlag.External;
        }

        private string SetExternalInclude(string subString) {
            return _isBaseFile ? subString : subString ?? _currentParsedFile;
        }

        /// <summary>
        /// Determines flags
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="lcFlagString"></param>
        /// <returns></returns>
        private static ParseFlag SetFlags(ParseFlag flag, string lcFlagString) {
            if (lcFlagString.Contains("global")) flag = flag | ParseFlag.Global;
            if (lcFlagString.Contains("shared")) flag = flag | ParseFlag.Shared;
            if (lcFlagString.Contains("private")) flag = flag | ParseFlag.Private;
            if (lcFlagString.Contains("new")) flag = flag | ParseFlag.Private;
            return flag;
        }

        /// <summary>
        /// To test if a proc or a function has too much char in it, because this would make the
        /// appbuilder unable to open it correctly
        /// </summary>
        /// <param name="startLine"></param>
        /// <param name="endLine"></param>
        /// <returns></returns>
        private bool HasTooMuchChar(int startLine, int endLine) {
            if (!_isBaseFile) return false;
            return NbExtraCharBetweenLines(startLine, endLine) > 0;
        }

        /// <summary>
        /// returns the number of chars between two lines in the current document
        /// </summary>
        /// <param name="startLine"></param>
        /// <param name="endLine"></param>
        /// <returns></returns>
        private static int NbExtraCharBetweenLines(int startLine, int endLine) {
            return (Npp.GetPositionFromLine(endLine) - Npp.GetPositionFromLine(startLine)) - Config.Instance.GlobalMaxNbCharInBlock;
        }

        #endregion

    }
}