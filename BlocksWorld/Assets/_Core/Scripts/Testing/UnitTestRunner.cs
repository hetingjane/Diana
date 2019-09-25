using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QA {
	
	public class UnitTestRunner : MonoBehaviour
	{
		void Start() {

			var timer = new System.Diagnostics.Stopwatch();
			timer.Start();
			
			UnitTest.RunUnitTest(new CWCNLP.ParserUnitTest());
			UnitTest.RunUnitTest(new CWCNLP.GrokUnitTest());
			
			Debug.Log("Unit tests took " + timer.Elapsed.Seconds + " seconds");
		}
	}

}