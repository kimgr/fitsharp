﻿// Copyright © 2011 Syterra Software Inc.
// This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License version 2.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

using fitSharp.Fit.Engine;
using fitSharp.Fit.Model;
using fitSharp.Fit.Operators;
using fitSharp.Machine.Engine;
using fitSharp.Machine.Model;

namespace fit.Operators {
    public class InvokeProcedure: CellOperator, InvokeOperator<Cell>
    {
        public bool CanInvoke(TypedValue instance, string memberName, Tree<Cell> parameters) {
            return Processor.Contains(new Procedure(memberName));
        }

        public TypedValue Invoke(TypedValue instance, string memberName, Tree<Cell> parameters) {
            var procedure = Processor.Load(new Procedure(memberName));
            return Invoke((Parse)procedure.Instance, instance, parameters);
        }

        private TypedValue Invoke(Parse procedure, TypedValue target, Tree<Cell> parameterValues) {
            var doFixture = new CellTree("fitlibrary.DoFixture");
            var fixture = Processor.Parse(typeof (Interpreter), target, doFixture).GetValue<FlowInterpreter>();

            var parameters = new Parameters(procedure.Parts, parameterValues);
            var body = procedure.Parts.More.Parts.Parts != null
                ? new CellTree(procedure.Parts.More.Parts.Parts.DeepCopy(parameters.Substitute, s => s.More, s => s.Parts).SiblingTrees)
                : new CellTree((Tree<Cell>)procedure.DeepCopy(
                    parameters.Substitute,
                    s => s == procedure ? null : s.More,
                    s => s == procedure ? s.Parts.More : s.Parts));

            Processor.TestStatus.PushReturn(TypedValue.Void);
            ExecuteProcedure(fixture, body);
            Processor.TestStatus.LastAction = Processor.ParseTree(typeof(StoryTestString), body).ValueString;
            return Processor.TestStatus.PopReturn();
        }

        static void ExecuteProcedure(FlowInterpreter flowInterpreter, Tree<Cell> body) {
            foreach (var table in body.Branches) {
                flowInterpreter.InterpretFlow(table);
            }
        }

        private class Parameters {
            private readonly Tree<Cell> names;
            private readonly Tree<Cell> values;
           
            public Parameters(Tree<Cell> names, Tree<Cell> values) {
                this.names = names;
                this.values = values;
            }

            public Parse Substitute(Parse source) {
                int i = 2;
                foreach (Tree<Cell> parameterValue in values.Branches) {
                    if (names.Branches[i].Value.Text == source.Value.Text) {
                        return ((Parse) parameterValue).DeepCopy(s => null, s=> s == parameterValue ? null : s.More, s => s.Parts);
                    }
                    i += 2;
                }
                return null;
            }
        }
    }
}
