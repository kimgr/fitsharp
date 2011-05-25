﻿// Copyright © 2011 Syterra Software Inc. Includes work by Object Mentor, Inc., © 2002 Cunningham & Cunningham, Inc.
// This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License version 2.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

using System;
using fit.Test.Acceptance;
using fitSharp.Fit.Operators;
using fitSharp.Fit.Service;
using fitSharp.Machine.Engine;
using fitSharp.Machine.Model;
using NUnit.Framework;
using TestStatus=fitSharp.Fit.Model.TestStatus;

namespace fit.Test.NUnit {

    public class CellOperatorTest {
        protected Service.Service service;
        protected StringFixture stringFixture;
        protected IntFixture intFixture;
        protected PersonFixture personFixture;

        public static bool IsMatch(ParseOperator<Cell> parseOperator, string input) {
            var processor = new CellProcessorBase();
            ((CellOperator) parseOperator).Processor = processor;
            return parseOperator.CanParse(typeof (string), TypedValue.Void, TestUtils.CreateCell(input));
        }

        public static bool IsMatch(CompareOperator<Cell> compareOperator, object instance, Type type, string value) {
            var processor = new CellProcessorBase();
            processor.AddOperator(new CompareDefault());
            return compareOperator.CanCompare(new TypedValue(instance, type), TestUtils.CreateCell(value));
        }

        public static bool IsMatch(ExecuteCommand command, InvokeCommandBase executor, Tree<Cell> parameters) {
            var processor = new CellProcessorBase();
            processor.AddOperator(new ParseMemberName());
            executor.Processor = processor;
            return executor.CanExecute(new ExecuteContext(command, null, new TypedValue("stuff")), new ExecuteParameters(parameters));
        }

        public void MakeStringFixture() {
            service = new Service.Service();
            stringFixture = new StringFixture { Processor = service };
        }

        public void MakePersonFixture() {
            service = new Service.Service();
            personFixture = new PersonFixture { Processor = service };
        }

        public void MakeIntFixture() {
            service = new Service.Service();
            intFixture = new IntFixture { Processor = service };
        }

        public static void AssertCellPasses(Parse cell)
        {
            Assert.AreEqual(TestStatus.Right, cell.GetAttribute(CellAttribute.Status));
        }

        public static void AssertCellFails(Parse cell)
        {
            Assert.AreEqual(TestStatus.Wrong, cell.GetAttribute(CellAttribute.Status));
        }

        public static void AssertValueInBody(Parse cell, string value)
        {
            Assert.IsTrue(cell.Body.IndexOf(value) > -1);
        }

        public static void AssertValuesInBody(Parse cell, string[] values)
        {
            foreach (string value in values)
            {
                AssertValueInBody(cell, value);
            }
        }

    }
}
