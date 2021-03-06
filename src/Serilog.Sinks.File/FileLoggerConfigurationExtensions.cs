﻿// Copyright 2013-2017 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.ComponentModel;
using System.Text;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Formatting.Json;
using Serilog.Sinks.File;

// ReSharper disable MethodOverloadWithOptionalParameter

namespace Serilog
{
    /// <summary>Extends <see cref="LoggerConfiguration"/> with methods to add file sinks.</summary>
    public static class FileLoggerConfigurationExtensions
    {
        const int DefaultRetainedFileCountLimit = 31; // A long month of logs
        const long DefaultFileSizeLimitBytes = 1L * 1024 * 1024 * 1024;
        const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        #region Obsolete

        /// <summary>
        /// Write log events to the specified file.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="path">Path to the file.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for
        /// events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level
        /// to be changed at runtime.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.
        /// the default is "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}".</param>
        /// <param name="fileSizeLimitBytes">The approximate maximum size, in bytes, to which a log file will be allowed to grow.
        /// For unrestricted growth, pass null. The default is 1 GB. To avoid writing partial events, the last event within the limit
        /// will be written in full even if it exceeds the limit.</param>
        /// <param name="buffered">Indicates if flushing to the output file can be buffered or not. The default
        /// is false.</param>
        /// <param name="shared">Allow the log file to be shared by multiple processes. The default is false.</param>
        /// <param name="flushToDiskInterval">If provided, a full disk flush will be performed periodically at the specified interval.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <remarks>The file will be written using the UTF-8 character set.</remarks>
        [Obsolete("New code should not be compiled against this obsolete overload"), EditorBrowsable(EditorBrowsableState.Never)]
        public static LoggerConfiguration File(
            this LoggerSinkConfiguration sinkConfiguration,
            string path,
            LogEventLevel restrictedToMinimumLevel,
            string outputTemplate,
            IFormatProvider formatProvider,
            long? fileSizeLimitBytes,
            LoggingLevelSwitch levelSwitch,
            bool buffered,
            bool shared,
            TimeSpan? flushToDiskInterval)
        {
            // ReSharper disable once RedundantArgumentDefaultValue
            return File(sinkConfiguration, path, restrictedToMinimumLevel, outputTemplate, formatProvider, fileSizeLimitBytes,
                levelSwitch, buffered, shared, flushToDiskInterval, RollingInterval.Infinite, false,
                null, null);
        }

        /// <summary>
        /// Write log events to the specified file.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="formatter">A formatter, such as <see cref="JsonFormatter"/>, to convert the log events into
        /// text for the file. If control of regular text formatting is required, use the other
        /// overload of <see cref="File(LoggerSinkConfiguration, string, LogEventLevel, string, IFormatProvider, long?, LoggingLevelSwitch, bool, bool, TimeSpan?)"/>
        /// and specify the outputTemplate parameter instead.
        /// </param>
        /// <param name="path">Path to the file.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for
        /// events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level
        /// to be changed at runtime.</param>
        /// <param name="fileSizeLimitBytes">The approximate maximum size, in bytes, to which a log file will be allowed to grow.
        /// For unrestricted growth, pass null. The default is 1 GB. To avoid writing partial events, the last event within the limit
        /// will be written in full even if it exceeds the limit.</param>
        /// <param name="buffered">Indicates if flushing to the output file can be buffered or not. The default
        /// is false.</param>
        /// <param name="shared">Allow the log file to be shared by multiple processes. The default is false.</param>
        /// <param name="flushToDiskInterval">If provided, a full disk flush will be performed periodically at the specified interval.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <remarks>The file will be written using the UTF-8 character set.</remarks>
        [Obsolete("New code should not be compiled against this obsolete overload"), EditorBrowsable(EditorBrowsableState.Never)]
        public static LoggerConfiguration File(
            this LoggerSinkConfiguration sinkConfiguration,
            ITextFormatter formatter,
            string path,
            LogEventLevel restrictedToMinimumLevel,
            long? fileSizeLimitBytes,
            LoggingLevelSwitch levelSwitch,
            bool buffered,
            bool shared,
            TimeSpan? flushToDiskInterval)
        {
            // ReSharper disable once RedundantArgumentDefaultValue
            return File(sinkConfiguration, formatter, path, restrictedToMinimumLevel, fileSizeLimitBytes, levelSwitch,
                buffered, shared, flushToDiskInterval, RollingInterval.Infinite, false, null, null);
        }

        #endregion

        /// <summary>Write log events to the specified file.</summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="path">Path to the file.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level to be changed at runtime.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink. The default is "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}".</param>
        /// <param name="fileSizeLimitBytes">The approximate maximum size, in bytes, to which a log file will be allowed to grow. For unrestricted growth, pass null. The default is 1 GB. To avoid writing partial events, the last event within the limit will be written in full even if it exceeds the limit.</param>
        /// <param name="buffered">Indicates if flushing to the output file can be buffered or not. The default is false.</param>
        /// <param name="shared">Allow the log file to be shared by multiple processes. The default is false.</param>
        /// <param name="flushToDiskInterval">If provided, a full disk flush will be performed periodically at the specified interval.</param>
        /// <param name="rollingInterval">The interval at which logging will roll over to a new file.</param>
        /// <param name="rollOnFileSizeLimit">If <code>true</code>, a new file will be created when the file size limit is reached. Filenames will have a number appended in the format <code>_NNN</code>, with the first filename given no number.</param>
        /// <param name="retainedFileCountLimit">The maximum number of log files that will be retained, including the current log file. For unlimited retention, pass null. The default is 31.</param>
        /// <param name="encoding">Character encoding used to write the text file. The default is UTF-8 without BOM.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <remarks>The file will be written using the UTF-8 character set.</remarks>
        public static LoggerConfiguration File(
            this LoggerSinkConfiguration sinkConfiguration,
            string                       path,
            LogEventLevel                restrictedToMinimumLevel = LevelAlias.Minimum,
            string                       outputTemplate           = DefaultOutputTemplate,
            IFormatProvider              formatProvider           = null,
            long?                        fileSizeLimitBytes       = DefaultFileSizeLimitBytes,
            LoggingLevelSwitch           levelSwitch              = null,
            bool                         buffered                 = false,
            bool                         shared                   = false,
            TimeSpan?                    flushToDiskInterval      = null,
            RollingInterval              rollingInterval          = RollingInterval.Infinite,
            bool                         rollOnFileSizeLimit      = false,
            int?                         retainedFileCountLimit   = DefaultRetainedFileCountLimit,
            Encoding                     encoding                 = null
        )
        {
            if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (outputTemplate == null) throw new ArgumentNullException(nameof(outputTemplate));

            ITextFormatter formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);

            return File(
                sinkConfiguration,
                formatter,
                path,
                restrictedToMinimumLevel,
                fileSizeLimitBytes    : fileSizeLimitBytes,
                levelSwitch           : levelSwitch,
                buffered              : buffered,
                shared                : shared,
                flushToDiskInterval   : flushToDiskInterval,
                rollingInterval       : rollingInterval,
                rollOnFileSizeLimit   : rollOnFileSizeLimit,
                retainedFileCountLimit: retainedFileCountLimit,
                encoding              : encoding
            );
        }

        /// <summary>Write log events to the specified file.</summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="logDirectoryPath">Path to the root log output directory.</param>
        /// <param name="logFilePathFormat">Custom .NET Date Time format string. Used to generate the log file names. Use quote-delimited values to include file-name extensions, prefixes and directory names. For example, <c>&quot;yyyy-MM'\Log-'yyyy-MM-dd'.log'&quot;</c> to get output like <c>&quot;...\2019-02\Log-2019-02-22.log&quot;</c>.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level to be changed at runtime.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink. The default is "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}".</param>
        /// <param name="fileSizeLimitBytes">The approximate maximum size, in bytes, to which a log file will be allowed to grow. For unrestricted growth, pass null. The default is 1 GB. To avoid writing partial events, the last event within the limit will be written in full even if it exceeds the limit.</param>
        /// <param name="buffered">Indicates if flushing to the output file can be buffered or not. The default is false.</param>
        /// <param name="shared">Allow the log file to be shared by multiple processes. The default is false.</param>
        /// <param name="flushToDiskInterval">If provided, a full disk flush will be performed periodically at the specified interval.</param>
        /// <param name="rollingInterval">The interval at which logging will roll over to a new file.</param>
        /// <param name="rollOnFileSizeLimit">If <code>true</code>, a new file will be created when the file size limit is reached. Filenames will have a number appended in the format <code>_NNN</code>, with the first filename given no number.</param>
        /// <param name="retainedFileCountLimit">The maximum number of log files that will be retained, including the current log file. For unlimited retention, pass null. The default is 31.</param>
        /// <param name="encoding">Character encoding used to write the text file. The default is UTF-8 without BOM.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <remarks>The file will be written using the UTF-8 character set.</remarks>
        public static LoggerConfiguration File(
            this LoggerSinkConfiguration sinkConfiguration,
            string                       logDirectoryPath,
            string                       logFilePathFormat,
            LogEventLevel                restrictedToMinimumLevel = LevelAlias.Minimum,
            string                       outputTemplate           = DefaultOutputTemplate,
            IFormatProvider              formatProvider           = null,
            long?                        fileSizeLimitBytes       = DefaultFileSizeLimitBytes,
            LoggingLevelSwitch           levelSwitch              = null,
            bool                         buffered                 = false,
            bool                         shared                   = false,
            TimeSpan?                    flushToDiskInterval      = null,
            RollingInterval              rollingInterval          = RollingInterval.Infinite,
            bool                         rollOnFileSizeLimit      = false,
            int?                         retainedFileCountLimit   = DefaultRetainedFileCountLimit,
            Encoding                     encoding                 = null
        )
        {
            if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));
            if (logDirectoryPath == null) throw new ArgumentNullException(nameof(logDirectoryPath));
            if (logFilePathFormat == null) throw new ArgumentNullException(nameof(logFilePathFormat));
            if (outputTemplate == null) throw new ArgumentNullException(nameof(outputTemplate));

            ITextFormatter formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);

            return File(
                sinkConfiguration,
                formatter,
                logDirectoryPath        : logDirectoryPath,
                logFilePathFormat       : logFilePathFormat,
                restrictedToMinimumLevel: restrictedToMinimumLevel,
                fileSizeLimitBytes      : fileSizeLimitBytes,
                levelSwitch             : levelSwitch,
                buffered                : buffered,
                shared                  : shared,
                flushToDiskInterval     : flushToDiskInterval,
                rollingInterval         : rollingInterval,
                rollOnFileSizeLimit     : rollOnFileSizeLimit,
                retainedFileCountLimit  : retainedFileCountLimit,
                encoding                : encoding
            );
        }

        /// <summary>
        /// Write log events to the specified file.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="formatter">A formatter, such as <see cref="JsonFormatter"/>, to convert the log events into
        /// text for the file. If control of regular text formatting is required, use the other
        /// overload of <see cref="File(LoggerSinkConfiguration, string, LogEventLevel, string, IFormatProvider, long?, LoggingLevelSwitch, bool, bool, TimeSpan?, RollingInterval, bool, int?, Encoding)"/>
        /// and specify the outputTemplate parameter instead.
        /// </param>
        /// <param name="path">Path to the file.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for
        /// events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level
        /// to be changed at runtime.</param>
        /// <param name="fileSizeLimitBytes">The approximate maximum size, in bytes, to which a log file will be allowed to grow.
        /// For unrestricted growth, pass null. The default is 1 GB. To avoid writing partial events, the last event within the limit
        /// will be written in full even if it exceeds the limit.</param>
        /// <param name="buffered">Indicates if flushing to the output file can be buffered or not. The default
        /// is false.</param>
        /// <param name="shared">Allow the log file to be shared by multiple processes. The default is false.</param>
        /// <param name="flushToDiskInterval">If provided, a full disk flush will be performed periodically at the specified interval.</param>
        /// <param name="rollingInterval">The interval at which logging will roll over to a new file.</param>
        /// <param name="rollOnFileSizeLimit">If <code>true</code>, a new file will be created when the file size limit is reached. Filenames 
        /// will have a number appended in the format <code>_NNN</code>, with the first filename given no number.</param>
        /// <param name="retainedFileCountLimit">The maximum number of log files that will be retained,
        /// including the current log file. For unlimited retention, pass null. The default is 31.</param>
        /// <param name="encoding">Character encoding used to write the text file. The default is UTF-8 without BOM.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <remarks>The file will be written using the UTF-8 character set.</remarks>
        public static LoggerConfiguration File(
            this LoggerSinkConfiguration sinkConfiguration,
            ITextFormatter               formatter,
            string                       path,
            LogEventLevel                restrictedToMinimumLevel = LevelAlias.Minimum,
            long?                        fileSizeLimitBytes       = DefaultFileSizeLimitBytes,
            LoggingLevelSwitch           levelSwitch              = null,
            bool                         buffered                 = false,
            bool                         shared                   = false,
            TimeSpan?                    flushToDiskInterval      = null,
            RollingInterval              rollingInterval          = RollingInterval.Infinite,
            bool                         rollOnFileSizeLimit      = false,
            int?                         retainedFileCountLimit   = DefaultRetainedFileCountLimit,
            Encoding                     encoding                 = null
        )
        {
            ILogEventSink fileSink = CreateFileSink(
                path,
                logFilePathFormat     : null,
                rollingInterval       : rollingInterval,
                formatter             : formatter,
                fileSizeLimitBytes    : fileSizeLimitBytes,
                retainedFileCountLimit: retainedFileCountLimit,
                encoding              : encoding,
                buffered              : buffered,
                shared                : shared,
                rollOnFileSizeLimit   : rollOnFileSizeLimit,
                propagateExceptions   : false
            );

            return ConfigureFile(
                addSink                 : sinkConfiguration.Sink,
                sink                    : fileSink,
                restrictedToMinimumLevel: restrictedToMinimumLevel,
                levelSwitch             : levelSwitch,
                flushToDiskInterval     : flushToDiskInterval
            );
        }

        /// <summary>
        /// Write log events to the specified file.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="formatter">A formatter, such as <see cref="JsonFormatter"/>, to convert the log events into
        /// text for the file. If control of regular text formatting is required, use the other
        /// overload of <see cref="File(LoggerSinkConfiguration, string, LogEventLevel, string, IFormatProvider, long?, LoggingLevelSwitch, bool, bool, TimeSpan?, RollingInterval, bool, int?, Encoding)"/>
        /// and specify the outputTemplate parameter instead.
        /// </param>
        /// <param name="logDirectoryPath">Path to the root log output directory.</param>
        /// <param name="logFilePathFormat">Custom .NET Date Time format string. Used to generate the log file names. Use quote-delimited values to include file-name extensions, prefixes and directory names. For example, <c>&quot;yyyy-MM'\Log-'yyyy-MM-dd'.log'&quot;</c> to get output like <c>&quot;...\2019-02\Log-2019-02-22.log&quot;</c>.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for
        /// events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level
        /// to be changed at runtime.</param>
        /// <param name="fileSizeLimitBytes">The approximate maximum size, in bytes, to which a log file will be allowed to grow.
        /// For unrestricted growth, pass null. The default is 1 GB. To avoid writing partial events, the last event within the limit
        /// will be written in full even if it exceeds the limit.</param>
        /// <param name="buffered">Indicates if flushing to the output file can be buffered or not. The default
        /// is false.</param>
        /// <param name="shared">Allow the log file to be shared by multiple processes. The default is false.</param>
        /// <param name="flushToDiskInterval">If provided, a full disk flush will be performed periodically at the specified interval.</param>
        /// <param name="rollingInterval">The interval at which logging will roll over to a new file.</param>
        /// <param name="rollOnFileSizeLimit">If <code>true</code>, a new file will be created when the file size limit is reached. Filenames 
        /// will have a number appended in the format <code>_NNN</code>, with the first filename given no number.</param>
        /// <param name="retainedFileCountLimit">The maximum number of log files that will be retained,
        /// including the current log file. For unlimited retention, pass null. The default is 31.</param>
        /// <param name="encoding">Character encoding used to write the text file. The default is UTF-8 without BOM.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <remarks>The file will be written using the UTF-8 character set.</remarks>
        public static LoggerConfiguration File(
            this LoggerSinkConfiguration sinkConfiguration,
            ITextFormatter               formatter,
            string                       logDirectoryPath,
            string                       logFilePathFormat,
            LogEventLevel                restrictedToMinimumLevel = LevelAlias.Minimum,
            long?                        fileSizeLimitBytes       = DefaultFileSizeLimitBytes,
            LoggingLevelSwitch           levelSwitch              = null,
            bool                         buffered                 = false,
            bool                         shared                   = false,
            TimeSpan?                    flushToDiskInterval      = null,
            RollingInterval              rollingInterval          = RollingInterval.Infinite,
            bool                         rollOnFileSizeLimit      = false,
            int?                         retainedFileCountLimit   = DefaultRetainedFileCountLimit,
            Encoding                     encoding                 = null
        )
        {
            ILogEventSink fileSink = CreateFileSink(
                logDirectoryPath      : logDirectoryPath,
                logFilePathFormat     : logFilePathFormat,
                rollingInterval       : rollingInterval,
                formatter             : formatter,
                fileSizeLimitBytes    : fileSizeLimitBytes,
                retainedFileCountLimit: retainedFileCountLimit,
                encoding              : encoding,
                buffered              : buffered,
                shared                : shared,
                rollOnFileSizeLimit   : rollOnFileSizeLimit,
                propagateExceptions   : false
            );

            return ConfigureFile(
                addSink                 : sinkConfiguration.Sink,
                sink                    : fileSink,
                restrictedToMinimumLevel: restrictedToMinimumLevel,
                levelSwitch             : levelSwitch,
                flushToDiskInterval     : flushToDiskInterval
            );
        }

        /// <summary>
        /// Write log events to the specified file.
        /// </summary>
        /// <param name                                                 ="sinkConfiguration">Logger sink configuration.</param>
        /// <param name                                                 ="path">Path to the file.</param>
        /// <param name                                                 ="restrictedToMinimumLevel">The minimum level for
        /// events passed through the sink. Ignored when <paramref name ="levelSwitch"/> is specified.</param>
        /// <param name                                                 ="levelSwitch">A switch allowing the pass-through minimum level
        /// to be changed at runtime.</param>
        /// <param name                                                 ="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name                                                 ="outputTemplate">A message template describing the format used to write to the sink.
        /// the default is "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}".</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <remarks>The file will be written using the UTF-8 character set.</remarks>
        public static LoggerConfiguration File(
            this LoggerAuditSinkConfiguration sinkConfiguration,
            string                            path,
            LogEventLevel                     restrictedToMinimumLevel  = LevelAlias.Minimum,
            string                            outputTemplate            = DefaultOutputTemplate,
            IFormatProvider                   formatProvider            = null,
            LoggingLevelSwitch                levelSwitch               = null
        )
        {
            if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (outputTemplate == null) throw new ArgumentNullException(nameof(outputTemplate));

            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
            return File(
                sinkConfiguration       : sinkConfiguration,
                formatter               : formatter,
                path                    : path,
                restrictedToMinimumLevel: restrictedToMinimumLevel,
                levelSwitch             : levelSwitch
            );
        }

        /// <summary>
        /// Write log events to the specified file.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="logDirectoryPath">Path to the root log output directory.</param>
        /// <param name="logFilePathFormat">Custom .NET Date Time format string. Used to generate the log file names. Use quote-delimited values to include file-name extensions, prefixes and directory names. For example, <c>&quot;yyyy-MM'\Log-'yyyy-MM-dd'.log'&quot;</c> to get output like <c>&quot;...\2019-02\Log-2019-02-22.log&quot;</c>.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for
        /// events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level
        /// to be changed at runtime.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.
        /// the default is "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}".</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <remarks>The file will be written using the UTF-8 character set.</remarks>
        public static LoggerConfiguration File(
            this LoggerAuditSinkConfiguration sinkConfiguration,
            string                            logDirectoryPath,
            string                            logFilePathFormat,
            LogEventLevel                     restrictedToMinimumLevel = LevelAlias.Minimum,
            string                            outputTemplate           = DefaultOutputTemplate,
            IFormatProvider                   formatProvider           = null,
            LoggingLevelSwitch                levelSwitch              = null
        )
        {
            if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));
            if (logDirectoryPath == null) throw new ArgumentNullException(nameof(logDirectoryPath));
            if (outputTemplate == null) throw new ArgumentNullException(nameof(outputTemplate));

            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
            return File( sinkConfiguration, formatter, logDirectoryPath: logDirectoryPath, logFilePathFormat: logFilePathFormat, restrictedToMinimumLevel: restrictedToMinimumLevel, levelSwitch: levelSwitch );
        }

        /// <summary>
        /// Write log events to the specified file.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="formatter">A formatter, such as <see cref="JsonFormatter"/>, to convert the log events into
        /// text for the file. If control of regular text formatting is required, use the other
        /// overload of <see cref="File(LoggerAuditSinkConfiguration, string, LogEventLevel, string, IFormatProvider, LoggingLevelSwitch)"/>
        /// and specify the outputTemplate parameter instead.
        /// </param>
        /// <param name="path">Path to the file.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for
        /// events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level
        /// to be changed at runtime.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <remarks>The file will be written using the UTF-8 character set.</remarks>
        public static LoggerConfiguration File(
            this LoggerAuditSinkConfiguration sinkConfiguration,
            ITextFormatter                    formatter,
            string                            path,
            LogEventLevel                     restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch                levelSwitch              = null
        )
        {
            ILogEventSink fileSink = CreateFileSink(
                logDirectoryPath      : path,
                logFilePathFormat     : null,
                rollingInterval       : RollingInterval.Infinite,
                formatter             : formatter,
                fileSizeLimitBytes    : null,
                retainedFileCountLimit: null,
                encoding              : null,
                buffered              : false,
                shared                : false,
                rollOnFileSizeLimit   : false,
                propagateExceptions   : true
            );

            return ConfigureFile(
                addSink                 : sinkConfiguration.Sink,
                sink                    : fileSink,
                restrictedToMinimumLevel: restrictedToMinimumLevel,
                levelSwitch             : levelSwitch,
                flushToDiskInterval     : null
            );
        }

        /// <summary>
        /// Write log events to the specified file.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="formatter">A formatter, such as <see cref="JsonFormatter"/>, to convert the log events into
        /// text for the file. If control of regular text formatting is required, use the other
        /// overload of <see cref="File(LoggerAuditSinkConfiguration, string, LogEventLevel, string, IFormatProvider, LoggingLevelSwitch)"/>
        /// and specify the outputTemplate parameter instead.
        /// </param>
        /// <param name="logDirectoryPath">Path to the root log output directory.</param>
        /// <param name="logFilePathFormat">Custom .NET Date Time format string. Used to generate the log file names. Use quote-delimited values to include file-name extensions, prefixes and directory names. For example, <c>&quot;yyyy-MM'\Log-'yyyy-MM-dd'.log'&quot;</c> to get output like <c>&quot;...\2019-02\Log-2019-02-22.log&quot;</c></param>
        /// <param name="restrictedToMinimumLevel">The minimum level for
        /// events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level
        /// to be changed at runtime.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        /// <remarks>The file will be written using the UTF-8 character set.</remarks>
        public static LoggerConfiguration File(
            this LoggerAuditSinkConfiguration sinkConfiguration,
            ITextFormatter                    formatter,
            string                            logDirectoryPath,
            string                            logFilePathFormat,
            LogEventLevel                     restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch                levelSwitch              = null
        )
        {
            ILogEventSink fileSink = CreateFileSink(
                logDirectoryPath      : logDirectoryPath,
                logFilePathFormat     : logFilePathFormat,
                rollingInterval       : RollingInterval.Infinite,
                formatter             : formatter,
                fileSizeLimitBytes    : null,
                retainedFileCountLimit: null,
                encoding              : null,
                buffered              : false,
                shared                : false,
                rollOnFileSizeLimit   : false,
                propagateExceptions   : true
            );

            return ConfigureFile(
                addSink                 : sinkConfiguration.Sink,
                sink                    : fileSink,
                restrictedToMinimumLevel: restrictedToMinimumLevel,
                levelSwitch             : levelSwitch,
                flushToDiskInterval     : null
            );
        }

        #region Private

        static ILogEventSink CreateFileSink( string logDirectoryPath, string logFilePathFormat, RollingInterval rollingInterval, ITextFormatter formatter, long? fileSizeLimitBytes, int? retainedFileCountLimit, Encoding encoding, bool buffered, bool shared, bool rollOnFileSizeLimit, bool propagateExceptions )
        {
            if( rollOnFileSizeLimit || rollingInterval != RollingInterval.Infinite )
            {
                return CreateRollingFileSink( logDirectoryPath, logFilePathFormat, rollingInterval, formatter, fileSizeLimitBytes, retainedFileCountLimit, encoding, buffered, shared, rollOnFileSizeLimit );
            }
            else
            {
                return CreateNonRollingFileSink( logDirectoryPath, formatter, fileSizeLimitBytes, buffered: buffered, shared: shared, propagateExceptions: propagateExceptions );
            }
        }

        static ILogEventSink CreateRollingFileSink( string logDirectoryPath, string logFilePathFormat, RollingInterval rollingInterval, ITextFormatter formatter, long? fileSizeLimitBytes, int? retainedFileCountLimit, Encoding encoding, bool buffered, bool shared, bool rollOnFileSizeLimit )
        {
            if (shared && buffered) throw new ArgumentException("Buffered writes are not available when file sharing is enabled.", nameof(buffered));
            if (retainedFileCountLimit.HasValue && retainedFileCountLimit < 1) throw new ArgumentException("At least one file must be retained.", nameof(retainedFileCountLimit));
            if (fileSizeLimitBytes.HasValue && fileSizeLimitBytes < 0) throw new ArgumentException("Negative value provided; file size limit must be non-negative.", nameof(fileSizeLimitBytes));
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));

            PathRoller pathRoller;
            if( logFilePathFormat == null )
            {
                pathRoller = PathRoller.CreateForLegacyPath( logDirectoryPath, rollingInterval );
            }
            else
            {
                pathRoller = PathRoller.CreateForFormattedPath( logDirectoryPath, logFilePathFormat, rollingInterval );
            }

            return new RollingFileSink(pathRoller, formatter, fileSizeLimitBytes, retainedFileCountLimit, encoding, buffered, shared, rollOnFileSizeLimit);
        }

        static ILogEventSink CreateNonRollingFileSink( string path, ITextFormatter formatter, long? fileSizeLimitBytes, bool buffered, bool shared, bool propagateExceptions )
        {
            if (shared && buffered) throw new ArgumentException("Buffered writes are not available when file sharing is enabled.", nameof(buffered));
            if (fileSizeLimitBytes.HasValue && fileSizeLimitBytes < 0) throw new ArgumentException("Negative value provided; file size limit must be non-negative.", nameof(fileSizeLimitBytes));
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));

            try
            {
#pragma warning disable 618
                if (shared)
                {
                    return new SharedFileSink(path, formatter, fileSizeLimitBytes);
                }
                else
                {
                    return new FileSink(path, formatter, fileSizeLimitBytes, buffered: buffered);
                }
#pragma warning restore 618
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Unable to open file sink for {0}: {1}", path, ex);

                if (propagateExceptions)
                    throw;

                return null;
            }
        }

        static LoggerConfiguration ConfigureFile(
            this Func<ILogEventSink, LogEventLevel, LoggingLevelSwitch, LoggerConfiguration> addSink,
            ILogEventSink sink,
            LogEventLevel restrictedToMinimumLevel,
            LoggingLevelSwitch levelSwitch,
            TimeSpan? flushToDiskInterval
        )
        {
            if (addSink == null) throw new ArgumentNullException(nameof(addSink));

            if( sink == null )
            {
                return addSink( new NullSink(), LevelAlias.Maximum, null );
            }
            else
            {
                if (flushToDiskInterval.HasValue)
                {
                    sink = new PeriodicFlushToDiskSink(sink, flushToDiskInterval.Value);
                }

                return addSink(sink, restrictedToMinimumLevel, levelSwitch);
            }
        }

        #endregion
    }
}
