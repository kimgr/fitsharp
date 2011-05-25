// Copyright � 2011 Syterra Software Inc.
// This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License version 2.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

using System;
using System.Collections;
using System.Collections.Generic;
using fit.Model;
using fitSharp.Fit.Engine;
using fitSharp.Fit.Model;
using fitSharp.Fit.Service;
using fitSharp.Machine.Model;

namespace fit.Operators {
    public interface ListMatchStrategy {
        bool IsOrdered { get; }
        TypedValue[] ActualValues(object theActualRow);
        bool IsExpectedSize(Parse theExpectedCells, object theActualRow);
        bool FinalCheck(TestStatus testStatus);
        bool SurplusAllowed {get;}
        bool CellMatches(TypedValue actualValue, Parse expectedCell, int columnNumber);
    }

    public class CellMatcher {
        public CellProcessor Processor { get; private set; }

        public CellMatcher(CellProcessor processor) {
            Processor = processor;
        }

        public virtual bool CellMatches(TypedValue actualValue, Parse expectedCell, int columnNumber) {
            return Processor.Compare(actualValue, expectedCell);
        }
    }

    public class ListMatcher {
        readonly CellProcessor processor;
        readonly ListMatchStrategy strategy;

        public ListMatcher(CellProcessor processor, ListMatchStrategy strategy) {
            this.strategy = strategy;
            this.processor = processor;
        }

        public bool IsEqual(object theActualValue, Parse theExpectedValueCell) {
            var actuals = new Actuals((IList)theActualValue, strategy);
            int expectedRow = 0;
            foreach (Parse currentRow in new CellRange(theExpectedValueCell.Parts.Parts.More).Cells) {
                int match = actuals.FindMatch(RowMatches, expectedRow, currentRow.Parts);
                if (match < 0 || (match != expectedRow && strategy.IsOrdered)) return false;
                expectedRow++;
            }
            return (actuals.UnmatchedCount == 0);
        }

        public bool MarkCell(object systemUnderTest, object theActualValue, Parse theTableRows) {
            var actuals = new Actuals((IList)theActualValue, strategy);
            if (theTableRows.More == null && actuals.UnmatchedCount == 0) {
                processor.TestStatus.MarkRight(theTableRows);
            }
            bool result = true;
            int expectedRow = 0;
            foreach (Parse currentRow in new CellRange(theTableRows.More).Cells) {
                try {
                    int match = actuals.FindMatch(RowMatches, expectedRow, currentRow.Parts);
                    if (match < 0) {
                        MarkAsIncorrect(currentRow, "missing");
                        result = false;
                    }
                    expectedRow++;
                }
                catch (Exception e) {
                    processor.TestStatus.MarkException(currentRow.Parts, e);
                    return false;
                }
            }
            if (actuals.UnmatchedCount > 0 && !strategy.SurplusAllowed) {
                actuals.ShowSurplus(processor, theTableRows.Last);
                result = false;
            }

            Parse markRow = theTableRows.More;
            for (int row = 0; row < expectedRow; row++) {
                if (strategy.IsOrdered && actuals.IsOutOfOrder(row)) {
                    MarkAsIncorrect(markRow, "out of order");
                    result = false;
                }
                else if (actuals.Match(row) != null) {
                    TypedValue[] actualValues = strategy.ActualValues(actuals.Match(row));
                    int i = 0;
                    foreach (Parse cell in new CellRange(markRow.Parts).Cells) {
                        if (actualValues[i].Type != typeof(void) || cell.Text.Length > 0) {
                             new CellOperationImpl(processor).Check(systemUnderTest, actualValues[i], cell);

                        }
                        i++;
                    }
                }
                markRow = markRow.More;
            }

            if (!strategy.FinalCheck(processor.TestStatus)) return false;
            return result;
        }

        void MarkAsIncorrect(Parse theRow, string theReason) {
            Parse firstCell = theRow.Parts;
            firstCell.SetAttribute(CellAttribute.Label, theReason);
            processor.TestStatus.MarkWrong(theRow);
        }

        bool RowMatches(Parse theExpectedCells, object theActualRow) {
            if (!strategy.IsExpectedSize(theExpectedCells, theActualRow)) return false;
            Parse expectedCell = theExpectedCells;
            int i = 0;
            foreach (TypedValue actualValue in strategy.ActualValues(theActualRow)) {
                if (actualValue.Type != typeof(void) || expectedCell.Text.Length > 0) {
                    if (!strategy.CellMatches(actualValue, expectedCell, i)) return false;
                }
                expectedCell = expectedCell.More;
                i++;
            }
            return true;
        }

        class Actuals {
            readonly List<ActualItem> myActuals;
            readonly ListMatchStrategy myStrategy;

            public delegate bool Matches(Parse theExpectedCells, object theActual);
            public Actuals(IList theActualValues, ListMatchStrategy theStrategy) {
                myActuals = new List<ActualItem>(theActualValues.Count);
                foreach (object actualValue in theActualValues) {
                    myActuals.Add(new ActualItem(actualValue));
                }
                myStrategy = theStrategy;
                UnmatchedCount = theActualValues.Count;
            }

            public int UnmatchedCount { get; private set; }

            public object Match(int theIndex) {
                foreach (ActualItem item in myActuals) {
                    if (item.MatchRow == theIndex) return item.Value;
                }
                return null;
            }

            public int FindMatch(Matches theMatcher, int theExpectedRow, Parse theExpectedCells) {
                int result = -1;
                if (myActuals.Count == 0) return result;
                int lastMatched = -1;
                for (int row = 0; row < myActuals.Count; row++) {
                    if (myActuals[row].MatchRow != null) {
                        lastMatched = row;
                        continue;
                    }
                    if (result == -1 && theMatcher(theExpectedCells, myActuals[row].Value)) {
                        result = row;
                    }
                }
                if (result > -1) {
                    myActuals[result].MatchRow = theExpectedRow;
                    UnmatchedCount--;
                } else {
                    myActuals.Insert(lastMatched + 1, new ActualItem(null, -1));
                }
                return result;
            }

            public void ShowSurplus(CellProcessor processor, Parse theLastRow) {
                Parse lastRow = theLastRow;
                for (int i = 0; i < myActuals.Count;) {
                    ActualItem surplus = myActuals[i];
                    if (surplus.MatchRow != null) {
                        i++;
                        continue;
                    }
                    Parse surplusRow = MakeSurplusRow(processor, surplus.Value);
                    lastRow.More = surplusRow;
                    lastRow = surplusRow;
                    myActuals.RemoveAt(i);
                }
            }

            public bool IsOutOfOrder(int theExpectedRow) {
                return
                    (theExpectedRow < myActuals.Count && myActuals[theExpectedRow].MatchRow != theExpectedRow &&
                     myActuals[theExpectedRow].MatchRow != -1);
            }

            Parse MakeSurplusRow(CellProcessor processor, object theSurplusRow) {
                Parse cells = null;
                foreach (TypedValue actualValue in myStrategy.ActualValues(theSurplusRow)) {
                    var cell = (Parse) processor.Compose(actualValue.IsVoid ? new TypedValue(string.Empty) : actualValue);
                    if (cells == null) {
                        cell.SetAttribute(CellAttribute.Label, "surplus");
                        cells = cell;
                    }
                    else
                        cells.Last.More = cell;
                }
                var row = new Parse("tr", null, cells, null);
                processor.TestStatus.MarkWrong(row);
                return row;
            }

            class ActualItem {
                public readonly object Value;
                public int? MatchRow;
                public ActualItem(object theValue) {
                    Value = theValue;
                    MatchRow = null;
                }
                public ActualItem(object theValue, int theMatchRow) {
                    Value = theValue;
                    MatchRow = theMatchRow;
                }
            }
        }

    }
}