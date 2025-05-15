using System.Net;

namespace eRaptors.Common
{
    public class Utility
    {
        public static string GetVisitorIPAddress(IHttpContextAccessor _httpContextAccessor, bool GetLan = false)
        {
            string visitorIPAddress = string.Empty;
            try
            {
                if (_httpContextAccessor is not null && _httpContextAccessor.HttpContext is not null)
                {
                    var httpContext = _httpContextAccessor.HttpContext;
                    visitorIPAddress = httpContext.Connection.RemoteIpAddress.ToString();

                    if (string.IsNullOrEmpty(visitorIPAddress))
                        visitorIPAddress = httpContext.Connection.LocalIpAddress.ToString();

                        if (string.IsNullOrEmpty(visitorIPAddress))
                            visitorIPAddress = _httpContextAccessor.HttpContext.Request.Host.ToString();

                            if (string.IsNullOrEmpty(visitorIPAddress) || visitorIPAddress.Trim() == "::1")
                            {
                                GetLan = true;
                                visitorIPAddress = string.Empty;
                            }

                    if (GetLan && string.IsNullOrEmpty(visitorIPAddress))
                    {
                        string stringHostName = Dns.GetHostName();
                        IPHostEntry ipHostEntries = Dns.GetHostEntry(stringHostName);
                        IPAddress[] arrIpAddress = ipHostEntries.AddressList;
                        try
                        {
                            visitorIPAddress = arrIpAddress[arrIpAddress.Length - 2].ToString();
                        }
                        catch
                        {
                            try
                            {
                                visitorIPAddress = arrIpAddress[0].ToString();
                            }
                            catch
                            {
                                try
                                {
                                    arrIpAddress = Dns.GetHostAddresses(stringHostName);
                                    visitorIPAddress = arrIpAddress[0].ToString();
                                }
                                catch
                                {
                                    visitorIPAddress = "127.0.0.1";
                                }
                            }
                        }

                    }

                    if (!string.IsNullOrEmpty(visitorIPAddress))
                    {
                        try
                        {
                            if (visitorIPAddress.Contains(":"))
                            {
                                var ipAddress = visitorIPAddress.Split(':')[0];
                                visitorIPAddress = ipAddress.ToString();

                            }
                        }
                        catch { }

                    }
                }
                else
                {
                    visitorIPAddress = string.Empty;
                }
            }
            catch (Exception)
            {
                visitorIPAddress = string.Empty;
            }
        
            return visitorIPAddress;
        }
    }
}
