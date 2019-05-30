using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QA {
	
	public class UnitTestRunner : MonoBehaviour
	{
		void Start() {
			UnitTest.RunUnitTest(new CWCNLP.ParserUnitTest());
		}
	}

}