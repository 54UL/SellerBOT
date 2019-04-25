using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Services
{
    public class PartsTechAPI
    {

        private string _sesion_id = new string("no sesion id");
        private string __partner_id = new string("beta_bosch");
        private HttpClient _client = new HttpClient();
        private DateTime logTime;
        private string _access_token;
        private string _refresh_token;
        private TimeSpan _expires_in;

        public PartsTechAPI()
        {
            this._cartExists = false;
            LogPartner().Wait();
        }

        //funciones de ayuda

        private async Task<dynamic> LogPartner()
        {
            var data = new
            {
                accessType = "user",
                credentials = new
                {
                    partner = new
                    {
                        id = "beta_bosch",
                        key = "4700fc1c26dd4e54ab26a0bc1c9dd40d"
                    },
                    user = new
                    {
                        id = "hackteam_18",
                        key = "e73908b462d547a69525ef5291368068"
                    }
                }
            };
            var contentString = JObject.FromObject(data).ToString();
            var dataString = new StringContent(contentString, Encoding.UTF8, "application/json");
            var url = "https://api.beta.partstech.com/oauth/access";
            logTime = DateTime.Now;
            var response = await this._client.PostAsync(url, dataString);
            dynamic result = null;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                try
                {
                    var content = await response.Content.ReadAsStringAsync();
                    result = JObject.Parse(content);
                    _access_token = result.accessToken;
                    _refresh_token = result.refreshToken;
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e.Message);
                }

                return result;
            }
            return null;
        }

        private async Task<string> refreshAcces()
        {
            if ((logTime - DateTime.Now).TotalSeconds <= _expires_in.TotalSeconds)
            {
                var url = "https://api.beta.partstech.com/oauth/refresh";
                var dataString = "{" + _refresh_token + "}";
                var stringContent = new StringContent(dataString, Encoding.UTF8, "application/json");
                var response = await this._client.PostAsync(url, stringContent);
                dynamic result = null;
                if (response != null)
                {
                    result = await JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                    _access_token = result.accessToken;
                    _refresh_token = result.refreshToken;
                    return "ok";
                }
                else
                {
                    return null;
                }
            }
            return "juanga";
        }

        private dynamic ReadJSON(Uri restURI)
        {
            dynamic jsonResponse = null;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(restURI);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader streamReader = new StreamReader(stream))
            {
                var json = streamReader.ReadToEnd();
                jsonResponse = JsonConvert.DeserializeObject<dynamic>(json);
            }

            return new JsonTextReader(new StringReader(jsonResponse));
        }

        //login / autorizacion

        public async Task<dynamic> AutorizeLogin(object credentialInfo)
        {
            var data = new { accessType = "user", credentials = credentialInfo };
            var contentString = JObject.FromObject(data).ToString();
            var dataString = new StringContent(contentString, Encoding.UTF8, "application/json");
            var url = "https://api.beta.partstech.com/oauth/access";
            logTime = DateTime.Now;
            var response = await this._client.PostAsync(url, dataString);
            dynamic result = null;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                try
                {
                    var content = await response.Content.ReadAsStringAsync();
                    result = JObject.Parse(content);
                    _access_token = result.accessToken;
                    _refresh_token = result.refreshToken;
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e.Message);
                }

                return result;
            }
            return null;
        }


        //Catalogo

        public string retriveBuyersGuide(string partID)
        {
            return new String("guide for a part");
        }
        /* 
        public async string    findProduct(dynamic paramms, string Filters)
        {

            return new String("there is no mechanism to find products");
        }
        */

        public string setShopingInformation()
        {
            return "return basic information of shipping";
        }
        //Compras

        private bool _cartExists;
        private string CreateCart(List<dynamic> objects, Uri objectUrl)
        {
            return "return quote";
        }

        public async Task<dynamic> GetItemByID(string partId)
        {
            HttpResponseMessage response = null;
            try
            {
                _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _access_token);
                response = await this._client.GetAsync($"https://api.beta.partstech.com/catalog/parts/{partId}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            return (response != null) ? JObject.Parse(await response.Content.ReadAsStringAsync()) : null;
        }


        #region CART_IMPLEMENTATION

        public async void AddNewPartToCart(dynamic content)
        {
            Uri uriAddPart = new Uri("https://api.partstech.com/punchout/cart/add-part");

            if (this._cartExists && content != null)
                await this._client.PostAsync(uriAddPart, content);
            else
            {
                CreateCart(content.ToList(), uriAddPart);
                AddNewPartToCart(content);
                this._cartExists = !this._cartExists;
            }

        }

        public async void DeletePartFromCart(dynamic content)
        {
            if (this._cartExists)
                await this._client.DeleteAsync("https://api.partstech.com/punchout/cart/remove-parts", content);
            else
                throw new Exception();
        }

        public async Task<dynamic> GetCartItems()
        {
            if (this._cartExists)
            {
                var response = await this._client.GetAsync("https://api.partstech.com/punchout/cart/info");
                return response;
            }

            return null;
        }

        public async Task<dynamic> GetVehicleByID(int vehicleID)
        {
            return await this._client.GetAsync($"https://api.partstech.com/taxonomy/vehicles/{vehicleID}");
        }

        public async Task<dynamic> GetShop()
        {
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _access_token);
            var requestStoreId = await this._client.GetAsync("https://api.beta.partstech.com/profile/shop");
            dynamic result = JObject.Parse(await requestStoreId.Content.ReadAsStringAsync());
            dynamic shop = new
            {
                id = result.id,
                name = result.name,
                phone = result.phone,
                cellphone = result.cellphone,
                address = result.billingAddress
            };
            return shop;
        }


        public async Task<dynamic> GetQuote(string partId)
        {
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _access_token);

            var requestStoreId = await this._client.GetAsync("https://api.beta.partstech.com/profile/shop/suppliers");
            var str = await requestStoreId.Content.ReadAsStringAsync();

            dynamic supplier = JArray.Parse(str);

            var _storeId = supplier[0].store.id;

            var cnt = new
            {
                searchParams = new
                {
                    partNumber = new string[] { partId }
                },
                storeId = _storeId
            };

            var request = new StringContent(JObject.FromObject(cnt).ToString());
            var response = await this._client.PostAsync("https://api.beta.partstech.com/catalog/quote", request);
            JObject result = null;
            try
            {
                str = await response.Content.ReadAsStringAsync();
                result = JObject.Parse(str);
            }
            catch (Exception e)
            {

                throw;
            }
            return JObject.Parse(await response.Content.ReadAsStringAsync());
        }


        #endregion
    }
}


