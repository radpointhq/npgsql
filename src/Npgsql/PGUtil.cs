using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Npgsql
{
    static class Statics
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Expect<T>(IBackendMessage msg)
            => msg is T asT
                ? asT
                : throw new NpgsqlException($"Received backend message {msg.Code} while expecting {typeof(T).Name}. Please file a bug.");
    }

    // ReSharper disable once InconsistentNaming
    static class PGUtil
    {
        internal static readonly UTF8Encoding UTF8Encoding = new UTF8Encoding(false, true);
        internal static readonly UTF8Encoding RelaxedUTF8Encoding = new UTF8Encoding(false, false);

        internal const int BitsInInt = sizeof(int) * 8;

        internal static void ValidateBackendMessageCode(BackendMessageCode code)
        {
            switch (code)
            {
            case BackendMessageCode.AuthenticationRequest:
            case BackendMessageCode.BackendKeyData:
            case BackendMessageCode.BindComplete:
            case BackendMessageCode.CloseComplete:
            case BackendMessageCode.CompletedResponse:
            case BackendMessageCode.CopyData:
            case BackendMessageCode.CopyDone:
            case BackendMessageCode.CopyBothResponse:
            case BackendMessageCode.CopyInResponse:
            case BackendMessageCode.CopyOutResponse:
            case BackendMessageCode.DataRow:
            case BackendMessageCode.EmptyQueryResponse:
            case BackendMessageCode.ErrorResponse:
            case BackendMessageCode.FunctionCall:
            case BackendMessageCode.FunctionCallResponse:
            case BackendMessageCode.NoData:
            case BackendMessageCode.NoticeResponse:
            case BackendMessageCode.NotificationResponse:
            case BackendMessageCode.ParameterDescription:
            case BackendMessageCode.ParameterStatus:
            case BackendMessageCode.ParseComplete:
            case BackendMessageCode.PasswordPacket:
            case BackendMessageCode.PortalSuspended:
            case BackendMessageCode.ReadyForQuery:
            case BackendMessageCode.RowDescription:
                return;
            default:
                throw new NpgsqlException("Unknown message code: " + code);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int RotateShift(int val, int shift)
            => (val << shift) | (val >> (BitsInInt - shift));

        internal static readonly Task CompletedTask = Task.FromResult(0);
        internal static readonly Task<bool> TrueTask = Task.FromResult(true);
        internal static readonly Task<bool> FalseTask = Task.FromResult(false);
        internal static readonly Task<int> CancelledTask = CreateCancelledTask<int>();

        static Task<T> CreateCancelledTask<T>()
        {
            var source = new TaskCompletionSource<T>();
            source.SetCanceled();
            return source.Task;
        }

        internal static StringComparer InvariantCaseIgnoringStringComparer => StringComparer.InvariantCultureIgnoreCase;

        internal static bool IsWindows =>
#if NET452
            Environment.OSVersion.Platform == PlatformID.Win32NT;
#else
            System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
#endif
    }

    enum FormatCode : short
    {
        Text = 0,
        Binary = 1
    }

    internal static class EnumerableExtensions
    {
        internal static string Join(this IEnumerable<string> values, string separator)
        {
            return string.Join(separator, values);
        }
    }

    /// <summary>
    /// Represents a timeout that will expire at some point.
    /// </summary>
    public readonly struct NpgsqlTimeout
    {
        readonly DateTime _expiration;
        internal DateTime Expiration => _expiration;

        internal static NpgsqlTimeout Infinite = new NpgsqlTimeout(TimeSpan.Zero);

        internal NpgsqlTimeout(TimeSpan expiration)
        {
            _expiration = expiration == TimeSpan.Zero
                ? DateTime.MaxValue
                : DateTime.UtcNow + expiration;
        }

        internal void Check()
        {
            if (HasExpired)
                throw new TimeoutException();
        }

        internal bool IsSet => _expiration != DateTime.MaxValue;

        internal bool HasExpired => DateTime.UtcNow >= Expiration;

        internal TimeSpan TimeLeft => IsSet ? Expiration - DateTime.UtcNow : Timeout.InfiniteTimeSpan;
    }

    sealed class CultureSetter : IDisposable
    {
        readonly CultureInfo _oldCulture;

        internal CultureSetter(CultureInfo newCulture)
        {
            _oldCulture = CultureInfo.CurrentCulture;
#if NET452
            Thread.CurrentThread.CurrentCulture = newCulture;
#else
            CultureInfo.CurrentCulture = newCulture;
#endif
        }

        public void Dispose()
        {
#if NET452
            Thread.CurrentThread.CurrentCulture = _oldCulture;
#else
            CultureInfo.CurrentCulture = _oldCulture;
#endif
        }
    }
}
