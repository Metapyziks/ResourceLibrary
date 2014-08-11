# ResourceLibrary

## Examples

### Register a resource type

    // Method to write a JObject to a stream
    public static void SaveJObject(Stream stream, JObject resource)
    {
        var writer = new JsonTextWriter(new StreamWriter(stream));
        resource.WriteTo(writer);
        writer.Flush();
    }

    // Method to read a JObject from a stream
    public static JObject LoadJObject(Stream stream)
    {
        var reader = new JsonTextReader(new StreamReader(stream));
        return JObject.Load(reader);
    }
    
    [ResourceTypeRegistration]
    public static void RegisterResourceTypes()
    {
        // Register JObject as a resource type;
        // * The resource should be compressed when stored in a resource file
        // * SaveJObject implements saving the object to a stream
        // * LoadJObject implements loading the object from a stream
        // * ".json" and ".cfg" files should be detected as containing a JObject
        Archive.Register<JObject>(ResourceFormat.Compressed, SaveJObject, LoadJObject, ".json", ".cfg");
    }

### Create a resource archive from a directory, and save it to a file

    var arch = Archive.FromDirectory("path/to/directory");
    arch.Save("path/to/archive.dat");
    
### Load an existing resource archive from a file

    var arch = Archive.FromFile("path/to/archive.dat");
    
### Mount and unmount archives, and get a resource from them

    // Look for resources in both existing resource file, and in
    // directory (the last archive mounted is checked first)
    var archives = new[] {
        Archive.FromFile("path/to/resource.dat"),
        Archive.FromDirectory("path/to/directory")
    };
    
    foreach (var arch in archives) arch.Mount();
    
    // Look for a json file at location/of/resource.json
    var obj = Archive.Get<JObject>((ResourceLocator) "location/of/resource");
    
    foreach (var arch in archives) arch.Unmount();
    
### List the locations of all resources of a given type in a directory recursively

    var locations = Archive.FindAll<JObject>(new ResourceLocator("location", "of", "directory"), true);
    
