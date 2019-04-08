// Notes: 

1. "System.Runtime.Serialization" -- This Assembly need to be added as a reference to this Project.
2. Solution Items Folder has three Dll's, which need to be referenced into the NC project.
	    I. B360.Notifier.Common.dll
	   II. log4net.dll
	  III. Newtonsoft.Json.dll
3. "AlarmProperties.xml" and "GlobalProperties.xml" files must be a embedded resource files.
		I. Right click on the File and click on the Properties.
		II. Make sure the "Build Action" is set to "Embedded Resource". 

4. Your Channel class must inherit the IChannelNotification Interface.
		I . Like : public class FileChannel : IChannelNotification