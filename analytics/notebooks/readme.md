# Sample Notebooks

## Prerequisites
You will need an [Azure Resource Group](https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/manage-resource-groups-portal#create-resource-groups).



## Creating a Storage Account
In your resource group, [create a Storage Account](https://docs.microsoft.com/en-us/azure/storage/blobs/create-data-lake-storage-account#enable-the-hierarchical-namespace).

In the [**Basics**](https://docs.microsoft.com/en-us/azure/storage/blobs/create-data-lake-storage-account#choose-a-storage-account-type) tab, fill in the appropriate values for the **Subscription** and **Resource group**. Set a **Storage account name** and pick a **Region**. For all other fields, use the default values.

Select **Next**.

In the [**Advanced**](https://docs.microsoft.com/en-us/azure/storage/blobs/create-data-lake-storage-account#enable-the-hierarchical-namespace) tab, look for the "Data Lake Storage Gen2" section. Check the box for **Enable hierarchical namespace**.

Select **Review + Create**. Select **Create**. 

Once the storage account has been successfully created, either select **Go to resource**, or find the storage account in your resource group.



## Creating an Azure Synapse Workspace
Follow the instructions here to create an [**Azure Synapse Workspace**](https://docs.microsoft.com/en-us/azure/synapse-analytics/get-started-create-workspace).

Note that on the **Basics** tab, there is a section to **Select Data Lake Storage Gen2**. For **Account name**, use the name of the **Storage Account** that was created earlier.

For **File system name**, select **Create new** and set "datalake" as the name. Be sure to select the **Assign myself the Storage Blob Data Contributor role on the Data Lake Storage Gen2 account** setting.

Select **Review + Create**. Select **Create**. 



## Using the Azure Synapse Workspace
Select **Go to resource group**. Select the Synapse Workspace that was just created.

Under the **Getting Started** section, there will be a section named **Open Synapse Studio**. Select the **Open** link.

In the future, either use this method or [open Azure Synapse Studio directly](https://docs.microsoft.com/en-us/azure/synapse-analytics/get-started-create-workspace#open-synapse-studio).



## Running the Sample Notebooks


### Importing the Sample Notebooks
Download the sample notebooks in their default ".ipynb" file format.

Expand the menu on the left side of the workspace. Select **Develop**. A sub-menu called **Develop** will appear. Select the + button and look for the **Import** option. Import the sample notebooks.


### Creating an Apache Spark Pool
On the left side menu of the Azure Synapse Workspace, select **Manage**.

A sub-menu will appear. Under the **Analytics pools** section, select **Apache Spark pools**.

Select **New**. Enter a value for **Apache Spark pool name**. 

For **Node size**, choose **Small (4 vCores / 32 GB)**. Disable **Autoscale**. For **Number of Nodes**, use 3.

For all other options, the default settings are appropriate.

Select **Review + Create**, then select **Create**.


### Running the Sample Notebooks
Select the **Develop** tab.

The Notebooks contain documentation cells with instructions on how to run each Notebook. This includes providing arguments for variables and running specific cells.    

The Sample Notebooks should be run in the following order :
- rate-streaming-to-bronze

The "rate-streaming-to-bronze" notebook must be run as a prerequisite to run the following :
- bronze-to-silver-telemetry    
- bronze-to-silver-vausage    

The "bronze-to-silver-vausage" notebook must be run as a prerequisite to run the following :
- CDM-silver-to-gold-vausage    
- silver-to-gold-vausage    
