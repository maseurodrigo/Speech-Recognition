using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Speech_Recognition.Data.Files
{
    public class FileData
    {
        private static String schemaPath, filePath;
        public FileData(String _schemaPath, String _filePath) {
            schemaPath = _schemaPath;
            filePath = _filePath;
        }

        // Check if JSON string its valid
        public bool isValidJson() {
            // Parse JSON schema
            JSchema jsonSchema = JSchema.Parse(File.ReadAllText(schemaPath));
            // Parse JSON data file
            JObject jsonData = JObject.Parse(File.ReadAllText(filePath));
            return jsonData.IsValid(jsonSchema);
        }

        // Pase JSON data into APIsData structure
        public APIsData getAPIsData() {
            try {
                return JsonConvert.DeserializeObject<APIsData>(File.ReadAllText(filePath));
            } catch (NullReferenceException) {
                return new APIsData();
            } catch (JsonReaderException) {
                return new APIsData();
            }
        }
    }
}