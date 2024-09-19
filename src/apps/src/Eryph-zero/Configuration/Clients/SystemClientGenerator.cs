﻿using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Eryph.Configuration;
using Eryph.Configuration.Model;
using Eryph.Core;
using Eryph.ModuleCore;
using Eryph.Security.Cryptography;

namespace Eryph.Runtime.Zero.Configuration.Clients
{
    public interface ISystemClientGenerator
    {
        Task EnsureSystemClient();
    }

    public class SystemClientGenerator(
        ICertificateGenerator certificateGenerator,
        ICertificateKeyPairGenerator certificateKeyPairGenerator,
        ICryptoIOServices cryptoIOServices,
        IConfigReaderService<ClientConfigModel> configReader,
        IConfigWriterService<ClientConfigModel> configWriter,
        IEndpointResolver endpointResolver)
        : ISystemClientGenerator
    {
        private static readonly string[] RequiredScopes = ["compute:write", "identity:write"];

        public async Task EnsureSystemClient()
        {
            var identityEndpoint = endpointResolver.GetEndpoint("identity");
            var entropy = Encoding.UTF8.GetBytes(identityEndpoint.ToString());
            var systemClientKeyFile = Path.Combine(ZeroConfig.GetClientConfigPath(), "system-client.key");

            var existingConfig = configReader.GetConfig()
                .FirstOrDefault(c => c.ClientId == EryphConstants.SystemClientId);
            
            using var existingPrivateKey = await cryptoIOServices.TryReadPrivateKeyFile(systemClientKeyFile, entropy);

            if (IsValid(existingConfig, existingPrivateKey))
                return;

            if (existingConfig is not null)
                await configWriter.Delete(existingConfig, EryphConstants.DefaultProjectName);

            using var keyPair = certificateKeyPairGenerator.GenerateRsaKeyPair(2048);
            var subjectNameBuilder = new X500DistinguishedNameBuilder();
            subjectNameBuilder.AddOrganizationName("eryph");
            subjectNameBuilder.AddOrganizationalUnitName("eryph-identity-client");
            subjectNameBuilder.AddCommonName(EryphConstants.SystemClientId);

            using var certificate = certificateGenerator.GenerateSelfSignedCertificate(
                subjectNameBuilder.Build(),
                keyPair,
                5 * 365,
                [
                    new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true),
                    new X509EnhancedKeyUsageExtension(
                        [Oid.FromFriendlyName("Client Authentication", OidGroup.EnhancedKeyUsage)],
                        true),
                ]);

            var config = new ClientConfigModel
            {
                ClientId = EryphConstants.SystemClientId,
                X509CertificateBase64 = Convert.ToBase64String(certificate.Export(X509ContentType.Cert)),
                AllowedScopes = RequiredScopes,
                TenantId = EryphConstants.DefaultTenantId,
                Roles = [EryphConstants.SuperAdminRole],
            };

            await configWriter.Add(config, EryphConstants.DefaultProjectName);
            await cryptoIOServices.WritePrivateKeyFile(systemClientKeyFile, keyPair, entropy);
        }

        private static bool IsValid(ClientConfigModel? configData, RSA? privateKey)
        {
            if(configData is null || privateKey is null)
                return false;

            if (!configData.AllowedScopes.SequenceEqual(RequiredScopes))
                return false;

            using var publicKey = GetPublicKey(configData.X509CertificateBase64);
            if (publicKey is null)
                return false;

            return CryptographicOperations.FixedTimeEquals(
                privateKey.ExportSubjectPublicKeyInfo(),
                publicKey.ExportSubjectPublicKeyInfo());
        }

        private static RSA? GetPublicKey(string certificateData)
        {
            try
            {
                using var certificate = new X509Certificate2(Convert.FromBase64String(certificateData));
                return certificate.GetRSAPublicKey();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}