using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureTrafficManagerApp.AvailabilityRepository
{
    public enum SupportedRepositories
    {
        Table
    }

    public static class AvailabilityRepositoryFactory
    {
        public static IAvailabilityRepository CreateRepository(SupportedRepositories repositoryToCreate, string connectionString)
        {
            switch (repositoryToCreate)
            {
                case SupportedRepositories.Table:
                    return Repositories.AvailabilityTableRepository.CreateRepository(connectionString);

                default:
                    throw new ArgumentException("Repository is not supported in this implementation!");
            }
        }
    }
}
