using Fathym.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace state_api_ide_management_tests
{
    [TestClass]
    public class SaveLCUTests : AzFunctionTestBase
    {
        
        public SaveLCUTests() : base()
        {
            APIRoute = "api/SaveLCU";                
        }

        [TestMethod]
        public async Task TestSaveLCU()
        {
            LcuEntApiKey = "";            
            PrincipalId = "";

            addRequestHeaders();

            var url = $"{HostURL}/{APIRoute}";            

            var response = await httpGet(url); 

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var model = getContent<dynamic>(response);

            dynamic result = model.Result;            

            throw new NotImplementedException("Implement me!");                  
        }
    }
}
