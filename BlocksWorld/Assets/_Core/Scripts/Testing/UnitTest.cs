/*
This is the base class for any unit test class we want to add elsewhere.
It also provides a bunch of static utility methods for asserting test results.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QA {

	public class UnitTest
	{
		public static void Fail(string message) {
			Debug.LogError("<color=red>Unit test failure:</color> " + message);
		}
		
		public static void Assert(bool condition, string message) {
			if (!condition) Fail(message);
		}
		
		public static void AssertTrue(bool condition, string message=null) {
			Assert(condition, message == null ? "Condition false when true was expected" : message);
		}
		
		public static void AssertFalse(bool condition, string message=null) {
			Assert(!condition, message == null ? "Condition true when false was expected" : message);
		}
		
		public static void AssertEqual(int expected, int actual, string message=null) {
			if (expected != actual) {
				if (message == null) message = "Expected " + expected + ", but actually " + actual;
				Fail(message);
			}
		}
		
		public static void AssertEqual(string expected, string actual, string message=null) {
			if (expected != actual) {
				if (message == null) message = "Expected \"" + expected + "\", but actually \"" + actual + "\"";
				Fail(message);
			}
		}
		
		public static void AssertEqual(Vector3 expected, Vector3 actual, float tolerance=1E-6f, string message=null) {
			if (Vector3.Distance(expected, actual) > tolerance) {
				if (message == null) message = "Expected \"" + expected + "\", but actually \"" + actual + "\"";
				Fail(message);
			}
		}

		// Virtual methods subclasses (specific unit test classes) will
		// want to override: at least Run(), and maybe Setup() and Cleanup() too.
		protected virtual void Setup() {}
		protected virtual void Run() {}
		protected virtual void Cleanup() {}
		
		public static void RunUnitTest(UnitTest test) {
			test.Setup();
			try {
				test.Run();
			} catch (System.Exception e) {
				Debug.LogError("<color=red>Unit test exception thrown:</color> " + e);
				Debug.LogError(e.StackTrace);
			}
			test.Cleanup();
		}
	}

}