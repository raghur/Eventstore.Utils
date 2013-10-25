namespace Eventstore.Utils
{
    public static class EventStoreConventionsHelper
    {
        public static string StorageConfigName(this string prefix) { return prefix + "-storage"; }
        public static string DefaultRouterQueue(this string prefix) { return prefix + "-route-cmd"; }
        public static string FunctionalEventRecorderQueue(this string prefix) { return prefix + "-route-events"; }
        public static string DefaultErrorsFolder(this string prefix) { return prefix + "-errors"; }

        public static string EventProcessingQueue(this string prefix) { return prefix + "-handle-events"; }
        public static string CommandHandlerQueue(this string prefix) { return prefix + "-handle-cmd"; }
        public static string QuarantineQueue(this string prefix) { return prefix + "-quarantined"; }

        public static string ViewsFolder(this string prefix) { return prefix + "-view"; }
        public static string DocsFolder(this string prefix) { return prefix + "-doc"; }
        public static string TaskHubName(this string prefix) { return prefix + "-taskhub"; }
    }
}