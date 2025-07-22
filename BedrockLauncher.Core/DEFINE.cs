using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Core
{
    public struct DEFINE
    {
        public static string COOKIE = "<Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://www.w3.org/2003/05/soap-envelope\">\r\n\t<Header>\r\n\t\t<Action d3p1:mustUnderstand=\"1\" xmlns:d3p1=\"http://www.w3.org/2003/05/soap-envelope\" xmlns=\"http://www.w3.org/2005/08/addressing\">http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetCookie</Action>\r\n\t\t<MessageID xmlns=\"http://www.w3.org/2005/08/addressing\">urn:uuid:b9b43757-2247-4d7b-ae8f-a71ba8a22386</MessageID>\r\n\t\t<To d3p1:mustUnderstand=\"1\" xmlns:d3p1=\"http://www.w3.org/2003/05/soap-envelope\" xmlns=\"http://www.w3.org/2005/08/addressing\">https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx</To>\r\n\t\t<Security d3p1:mustUnderstand=\"1\" xmlns:d3p1=\"http://www.w3.org/2003/05/soap-envelope\" xmlns=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">\r\n\t\t\t<Timestamp xmlns=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\">\r\n\t\t\t\t<Created>2017-12-02T00:16:15.210Z</Created>\r\n\t\t\t\t<Expires>2017-12-29T06:25:43.943Z</Expires>\r\n\t\t\t</Timestamp>\r\n\t\t\t<WindowsUpdateTicketsToken d4p1:id=\"ClientMSA\" xmlns:d4p1=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" xmlns=\"http://schemas.microsoft.com/msus/2014/10/WindowsUpdateAuthorization\">\r\n\t\t\t\t<TicketType Name=\"MSA\" Version=\"1.0\" Policy=\"MBI_SSL\">\r\n\t\t\t\t\t<User />\r\n\t\t\t\t</TicketType>\r\n\t\t\t</WindowsUpdateTicketsToken>\r\n\t\t</Security>\r\n\t</Header>\r\n\t<Body>\r\n\t\t<GetCookie xmlns=\"http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService\">\r\n\t\t\t<oldCookie>\r\n\t\t\t</oldCookie>\r\n\t\t\t<lastChange>2015-10-21T17:01:07.1472913Z</lastChange>\r\n\t\t\t<currentTime>2017-12-02T00:16:15.217Z</currentTime>\r\n\t\t\t<protocolVersion>1.40</protocolVersion>\r\n\t\t</GetCookie>\r\n\t</Body>\r\n</Envelope>";

    }
}
