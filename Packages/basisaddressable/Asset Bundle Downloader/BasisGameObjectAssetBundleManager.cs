using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Basis.Scripts.Addressable_Driver.Loading;
using static Basis.Scripts.Addressable_Driver.Loading.AddressableManagement;
public static class BasisGameObjectAssetBundleManager
{
    public static async Task<GameObject> DownloadAndLoadGameObjectAsync(string url, string Hash, string assetName, string subfolderName, ProgressReport progressCallback)
    {
        string folderPath = Path.Combine(Application.persistentDataPath, subfolderName);
        if (Directory.Exists(folderPath) == false)
        {
            Directory.CreateDirectory(folderPath);
        }
        string localPath = Path.Combine(folderPath, AddressableManagement.GetFileNameFromUrl(url));

        return await CheckAndLoadGameObjectBundleAsync(url, Hash,localPath, assetName, progressCallback);
    }

    private static async Task<GameObject> CheckAndLoadGameObjectBundleAsync(string url,string Hash, string localPath, string assetName, ProgressReport progressCallback)
    {
        if (File.Exists(localPath) == false)
        {
            Debug.Log("AssetBundle not found locally, downloading.");
            await DownloadAssetBundleAsync(url, localPath, progressCallback);
        }
        return await LoadGameObjectBundleFromDiskAsync(url, Hash, localPath, assetName, progressCallback);
    }
    private static async Task<GameObject> LoadGameObjectBundleFromDiskAsync(string url,string Hash, string localPath, string assetName, ProgressReport progressCallback)
    {
        Debug.Log("Loading Bundle");
        BasisLoadedAssets BasisLoadedAssets = await LoadBundle(url, Hash, localPath, progressCallback);
        if(BasisLoadedAssets == null)
        {
            return null;
        }
        if (BasisLoadedAssets.Bundle != null)
        {
            Debug.Log("AssetBundle loaded successfully from disk.");

            GameObject asset = BasisLoadedAssets.Bundle.LoadAsset<GameObject>(assetName);
            if (asset != null)
            {
                progressCallback?.Invoke(100); // Set progress to 100 when done
                return UnityEngine.Object.Instantiate(asset);
            }
            else
            {
                Debug.LogError("cant find Gameobject with name " + assetName);
            }

            Debug.LogError("Failed to load the specified GameObject from AssetBundle.");
        }
        else
        {
            if(BasisLoadedAssets.IsBundleLoaded)
            {
                Debug.LogError("Failed to load AssetBundle from disk but its marked as loaded with loaded Amount " + BasisLoadedAssets.ProgressReportAvatarLoad);
            }
            else
            {
                Debug.LogError("Failed to load AssetBundle from disk. was marked as bundle not loaded");
            }
        }

        return null;
    }
}