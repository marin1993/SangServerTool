using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SangServerTool.Tool
{
    public class GetCert
    {
        private ILogger _logger;

        public GetCert(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<int> Run(string config_file)
        {
            _logger.LogInformation($"��ʼִ�У�{DateTime.Now.ToString()}");
            _logger.LogInformation($"�����ļ���{config_file}");
            if (!File.Exists(config_file))
            {
                _logger.LogError("�����ļ�������");
                return 1;
            }
            IConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(config_file, optional: false, reloadOnChange: false);
            IConfigurationRoot config = configBuilder.Build();

            // ��ȡ���õ�Դվ����Ϣ
            var site = config["Certificate:site"];
            if (string.IsNullOrEmpty(site))
            {
                _logger.LogError("δ����Դվ����Ϣ");
                return 1;
            }
            // ����Ƿ�Ϊ�Ϸ���https URL
            if (!site.StartsWith("https://"))
            {
                _logger.LogError("Դվ����Ϣ����httpsվ��");
                return 1;
            }

            // ��ȡ�洢֤��λ��
            var cert_file = config["Certificate:cerpath"];
            if (string.IsNullOrEmpty(cert_file))
            {
                _logger.LogError("δ����֤��洢λ��");
                return 1;
            }

            // ��ȡԶ��֤��
            var cert = await GetRemoteCert(site);
            if (cert == null)
            {
                _logger.LogError("��ȡԶ��֤��ʧ��");
                return 1;
            }

            // ����֤��
            try
            {
                File.WriteAllText(cert_file, $"-----BEGIN CERTIFICATE-----\n{cert}\n-----END CERTIFICATE-----");
                _logger.LogInformation($"֤���ѱ��浽��{cert_file}");

                // shell�ű�
                var shell = config["Certificate:okshell"];
                if (!string.IsNullOrEmpty(shell))
                {
                    Utils.RunShell(shell, _logger);
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"����֤��ʧ�ܣ�{ex.Message}");
                return 1;
            }

        }

        private byte[] _certificate;
        private async Task<string?> GetRemoteCert(string site)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = CertificateValidationCallback
                };

                using (var client = new HttpClient(handler))
                {
                    await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, site));
                }

                return Convert.ToBase64String(_certificate, Base64FormattingOptions.InsertLineBreaks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"��ȡ֤��ʧ��: {ex.Message}");
                return null;
            }
        }

        private bool CertificateValidationCallback(HttpRequestMessage requestMessage, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            _logger.LogInformation($"֤����Ϣ: {certificate.Subject}");
            _certificate = certificate.GetRawCertData();
            return true;
        }
    }
}
