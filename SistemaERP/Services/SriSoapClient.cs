using System.Text;

namespace SistemaERP.Services
{
    public class SriSoapClient
    {
        private static readonly HttpClient client = new HttpClient();

        public async Task<string> EnviarComprobante(string base64Xml, string url)
        {
            string soapEnvelope = 
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
                                  "xmlns:ec=\"http://ec.gob.sri.ws.recepcion\">" +
                  "<soapenv:Header/>" +
                  "<soapenv:Body>" +
                    "<ec:validarComprobante>" +
                      "<xml>" + base64Xml + "</xml>" +
                    "</ec:validarComprobante>" +
                  "</soapenv:Body>" +
                "</soapenv:Envelope>";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            request.Headers.Add("SOAPAction", "\"\"");
            
            using var response = await client.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> ConsultarAutorizacion(string claveAcceso, string url)
        {
            string soapEnvelope = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ec=""http://ec.gob.sri.ws.autorizacion"">
                                    <soapenv:Header/>
                                    <soapenv:Body>
                                        <ec:autorizacionComprobante>
                                            <claveAccesoComprobante>{claveAcceso}</claveAccesoComprobante>
                                        </ec:autorizacionComprobante>
                                    </soapenv:Body>
                                </soapenv:Envelope>";

            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            var response = await client.PostAsync(url, content);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
