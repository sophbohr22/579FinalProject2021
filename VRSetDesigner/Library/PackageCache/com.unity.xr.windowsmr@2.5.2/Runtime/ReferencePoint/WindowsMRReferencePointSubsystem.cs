using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

#if ENABLE_WINMD_SUPPORT
using Windows.Perception.Spatial;
using Windows.Storage.Streams;
#endif

namespace UnityEngine.XR.WindowsMR
{
    static class NativeApi
    {
#if UNITY_EDITOR
        [DllImport("Packages/com.unity.xr.windowsmr/Runtime/Plugins/x64/WindowsMRXRSDK.dll", CharSet = CharSet.Auto)]
#elif ENABLE_DOTNET
        [DllImport("WindowsMRXRSDK.dll")]
#else
        [DllImport("WindowsMRXRSDK", CharSet=CharSet.Auto)]
#endif
        public static extern void UnityWindowsMR_refPoints_start();

#if UNITY_EDITOR
        [DllImport("Packages/com.unity.xr.windowsmr/Runtime/Plugins/x64/WindowsMRXRSDK.dll", CharSet = CharSet.Auto)]
#elif ENABLE_DOTNET
        [DllImport("WindowsMRXRSDK.dll")]
#else
        [DllImport("WindowsMRXRSDK", CharSet=CharSet.Auto)]
#endif
        public static extern void UnityWindowsMR_refPoints_stop();

#if UNITY_EDITOR
        [DllImport("Packages/com.unity.xr.windowsmr/Runtime/Plugins/x64/WindowsMRXRSDK.dll", CharSet = CharSet.Auto)]
#elif ENABLE_DOTNET
        [DllImport("WindowsMRXRSDK.dll")]
#else
        [DllImport("WindowsMRXRSDK", CharSet=CharSet.Auto)]
#endif
        public static extern void UnityWindowsMR_refPoints_onDestroy();

#if UNITY_EDITOR
        [DllImport("Packages/com.unity.xr.windowsmr/Runtime/Plugins/x64/WindowsMRXRSDK.dll", CharSet = CharSet.Auto)]
#elif ENABLE_DOTNET
        [DllImport("WindowsMRXRSDK.dll")]
#else
        [DllImport("WindowsMRXRSDK", CharSet=CharSet.Auto)]
#endif
        public static extern unsafe void* UnityWindowsMR_refPoints_acquireChanges(
            out void* addedPtr, out int addedCount,
            out void* updatedPtr, out int updatedCount,
            out void* removedPtr, out int removedCount,
            out int elementSize);

#if UNITY_EDITOR
        [DllImport("Packages/com.unity.xr.windowsmr/Runtime/Plugins/x64/WindowsMRXRSDK.dll", CharSet = CharSet.Auto)]
#elif ENABLE_DOTNET
        [DllImport("WindowsMRXRSDK.dll")]
#else
        [DllImport("WindowsMRXRSDK", CharSet=CharSet.Auto)]
#endif
        public static extern unsafe void UnityWindowsMR_refPoints_releaseChanges(
            void* changes);

#if UNITY_EDITOR
        [DllImport("Packages/com.unity.xr.windowsmr/Runtime/Plugins/x64/WindowsMRXRSDK.dll", CharSet = CharSet.Auto)]
#elif ENABLE_DOTNET
        [DllImport("WindowsMRXRSDK.dll")]
#else
        [DllImport("WindowsMRXRSDK", CharSet=CharSet.Auto)]
#endif
        public static extern bool UnityWindowsMR_refPoints_tryAdd(
            Pose pose,
            out XRReferencePoint referencePoint);

#if UNITY_EDITOR
        [DllImport("Packages/com.unity.xr.windowsmr/Runtime/Plugins/x64/WindowsMRXRSDK.dll", CharSet = CharSet.Auto)]
#elif ENABLE_DOTNET
        [DllImport("WindowsMRXRSDK.dll")]
#else
        [DllImport("WindowsMRXRSDK", CharSet=CharSet.Auto)]
#endif
        public static extern bool UnityWindowsMR_refPoints_tryRemove(TrackableId referencePointId);
    }

    /// <summary>
    /// The WindowsMR implementation of the <c>XRReferencePointSubsystem</c>. Do not create this directly.
    /// Use <c>XRReferencePointSubsystemDescriptor.Create()</c> instead.
    /// </summary>
    [Preserve]
    public sealed class WindowsMRReferencePointSubsystem : XRReferencePointSubsystem
    {
        protected override IProvider CreateProvider()
        {
            return new Provider();
        }

        class Provider : IProvider
        {
            public override void Start()
            {
                NativeApi.UnityWindowsMR_refPoints_start();
            }

            public override void Stop()
            {
                NativeApi.UnityWindowsMR_refPoints_stop();
            }

            public override void Destroy()
            {
                NativeApi.UnityWindowsMR_refPoints_onDestroy();
            }

            public override unsafe TrackableChanges<XRReferencePoint> GetChanges(
                XRReferencePoint defaultReferencePoint,
                Allocator allocator)
            {
                int addedCount, updatedCount, removedCount, elementSize;
                void* addedPtr, updatedPtr, removedPtr;
                var context = NativeApi.UnityWindowsMR_refPoints_acquireChanges(
                    out addedPtr, out addedCount,
                    out updatedPtr, out updatedCount,
                    out removedPtr, out removedCount,
                    out elementSize);

                try
                {
                    // Yes, this is an extra copy, but the hit is small compared with the code needed to get rid of it.
                    // If this becomes a problem we can eliminate the extra copy by doing something similar to
                    // NativeCopyUtility.PtrToNativeArrayWithDefault only with a pre-allocated array properties
                    // from using the TrackableChanges(int, int, int allocator) constructor.
                    var added = NativeCopyUtility.PtrToNativeArrayWithDefault<XRReferencePoint>(defaultReferencePoint, addedPtr, elementSize, addedCount, allocator);
                    var updated = NativeCopyUtility.PtrToNativeArrayWithDefault<XRReferencePoint>(defaultReferencePoint, updatedPtr, elementSize, updatedCount, allocator);
                    var removed = NativeCopyUtility.PtrToNativeArrayWithDefault<TrackableId>(default(TrackableId), removedPtr, elementSize, removedCount, allocator);


                    var ret = TrackableChanges<XRReferencePoint>.CopyFrom(
                        added,
                        updated,
                        removed,
                        allocator);

                    added.Dispose();
                    updated.Dispose();
                    removed.Dispose();
                    return ret;

                }
                finally
                {
                    NativeApi.UnityWindowsMR_refPoints_releaseChanges(context);
                }
            }

            public override bool TryAddReferencePoint(
                Pose pose,
                out XRReferencePoint referencePoint)
            {
                return NativeApi.UnityWindowsMR_refPoints_tryAdd(pose, out referencePoint);
            }

            public override bool TryRemoveReferencePoint(TrackableId referencePointId)
            {
                return NativeApi.UnityWindowsMR_refPoints_tryRemove(referencePointId);
            }

        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            XRReferencePointSubsystemDescriptor.Create(new XRReferencePointSubsystemDescriptor.Cinfo
            {
                id = "Windows Mixed Reality Reference Point",
                subsystemImplementationType = typeof(WindowsMRReferencePointSubsystem),
                supportsTrackableAttachments = false
            });
        }
    }
}
