# SMTP-Notification-Channel
The BizTalk360 Notification Channel has the capability to add the customized New Notification channels into BizTalk360. Which can be configured and the Notification will be triggered to your desired channel. You can download this SMTP Notification Channel for reference and start building your Own Channel based on this.

NOTES:

"System.Runtime.Serialization" -- This Assembly need to be added as a reference to this Project.

Solution Items Folder has three Dll's, which need to be referenced into the NC project.

I. B360.Notifier.Common.dll
II. log4net.dll
III. Newtonsoft.Json.dll
"AlarmProperties.xml" and "GlobalProperties.xml" files must be a embedded resource files.

 I. Right click on the File and click on the Properties.
II. Make sure the "Build Action" is set to "Embedded Resource".
Your Channel class must inherit the IChannelNotification Interface.

 I . Like : public class FileChannel : IChannelNotification
