﻿//------------------------------------------------------------------------------
// <auto-generated>
//     このコードはツールによって生成されました。
//     ランタイム バージョン:4.0.30319.42000
//
//     このファイルへの変更は、以下の状況下で不正な動作の原因になったり、
//     コードが再生成されるときに損失したりします。
// </auto-generated>
//------------------------------------------------------------------------------

namespace BurageSnap.Properties {
    using System;
    
    
    /// <summary>
    ///   ローカライズされた文字列などを検索するための、厳密に型指定されたリソース クラスです。
    /// </summary>
    // このクラスは StronglyTypedResourceBuilder クラスが ResGen
    // または Visual Studio のようなツールを使用して自動生成されました。
    // メンバーを追加または削除するには、.ResX ファイルを編集して、/str オプションと共に
    // ResGen を実行し直すか、または VS プロジェクトをビルドし直します。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   このクラスで使用されているキャッシュされた ResourceManager インスタンスを返します。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BurageSnap.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   厳密に型指定されたこのリソース クラスを使用して、すべての検索リソースに対し、
        ///   現在のスレッドの CurrentUICulture プロパティをオーバーライドします。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   型 System.Drawing.Bitmap のローカライズされたリソースを検索します。
        /// </summary>
        internal static System.Drawing.Bitmap cogs {
            get {
                object obj = ResourceManager.GetObject("cogs", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   型 System.Drawing.Bitmap のローカライズされたリソースを検索します。
        /// </summary>
        internal static System.Drawing.Bitmap folder_open {
            get {
                object obj = ResourceManager.GetObject("folder_open", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   型 System.Drawing.Bitmap のローカライズされたリソースを検索します。
        /// </summary>
        internal static System.Drawing.Bitmap folder_open_16 {
            get {
                object obj = ResourceManager.GetObject("folder_open_16", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Start に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormMain_buttonCapture_Click_Start {
            get {
                return ResourceManager.GetString("FormMain_buttonCapture_Click_Start", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Stop に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormMain_buttonCapture_Click_Stop {
            get {
                return ResourceManager.GetString("FormMain_buttonCapture_Click_Stop", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Capture に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormMain_checkBoxContinuous_CheckedChanged_Capture {
            get {
                return ResourceManager.GetString("FormMain_checkBoxContinuous_CheckedChanged_Capture", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Interval must be in the range of 1 ms to 1000 sec. に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string OptionDialog_textBoxInterval_Validating_Interval {
            get {
                return ResourceManager.GetString("OptionDialog_textBoxInterval_Validating_Interval", resourceCulture);
            }
        }
        
        /// <summary>
        ///   The size of the ring buffer must be in the range of 0 to 100. に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string OptionDialog_textBoxRingBuffer_Validating {
            get {
                return ResourceManager.GetString("OptionDialog_textBoxRingBuffer_Validating", resourceCulture);
            }
        }
    }
}
