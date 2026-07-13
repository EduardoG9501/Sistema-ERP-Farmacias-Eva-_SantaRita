using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Text;

namespace SistemaERP.Services
{
    public class SriNativeSigner
    {
        public static string SignXml(string xmlPath, string pfxPath, string password)
        {
            try
            {
                // 1. Cargar el documento XML
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(xmlPath);

                // 2. Cargar el certificado
                X509Certificate2 cert = new X509Certificate2(pfxPath, password, X509KeyStorageFlags.Exportable);

                // 3. Crear el objeto SignedXml
                SignedXml signedXml = new SignedXml(xmlDoc);
                signedXml.SigningKey = cert.GetRSAPrivateKey();

                // 4. Configurar la referencia
                Reference reference = new Reference();
                reference.Uri = ""; // Firma todo el documento
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signedXml.AddReference(reference);

                // 5. Configurar KeyInfo
                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause(new KeyInfoX509Data(cert));
                signedXml.KeyInfo = keyInfo;

                // 6. Calcular la firma
                signedXml.ComputeSignature();

                // 7. Insertar la firma en el XML
                XmlElement xmlDigitalSignature = signedXml.GetXml();
                xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xmlDigitalSignature, true));

                // 8. Guardar el archivo firmado (sobrescribir o nuevo)
                string signedPath = xmlPath.Replace(".xml", "_firmado.xml");
                xmlDoc.Save(signedPath);

                return signedPath;
            }
            catch (Exception ex)
            {
                throw new Exception("Error en firma nativa: " + ex.Message);
            }
        }
    }
}
