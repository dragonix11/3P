﻿#region Header
// // ========================================================================
// // Copyright (c) 2015 - Julien Caillon (julien.caillon@gmail.com)
// // This file (LexerVisitor.cs) is part of 3P.
// 
// // 3P is a free software: you can redistribute it and/or modify
// // it under the terms of the GNU General Public License as published by
// // the Free Software Foundation, either version 3 of the License, or
// // (at your option) any later version.
// 
// // 3P is distributed in the hope that it will be useful,
// // but WITHOUT ANY WARRANTY; without even the implied warranty of
// // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// // GNU General Public License for more details.
// 
// // You should have received a copy of the GNU General Public License
// // along with 3P. If not, see <http://www.gnu.org/licenses/>.
// // ========================================================================
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _3PA.MainFeatures.Parser {
    public class LexerVisitor : ILexerVisitor {
        public void Visit(TokenComment tok) {

        }

        public void Visit(TokenEol tok) {

        }

        public void Visit(TokenEos tok) {

        }

        public void Visit(TokenInclude tok) {

        }

        public void Visit(TokenNumber tok) {

        }

        public void Visit(TokenQuotedString tok) {

        }

        public void Visit(TokenSymbol tok) {

        }

        public void Visit(TokenEof tok) {

        }

        public void Visit(TokenWord tok) {

        }

        public void Visit(TokenWhiteSpace tok) {

        }

        public void Visit(TokenUnknown tok) {

        }

        public void Visit(TokenPreProcStatement tok) {
            
        }
    }
}
