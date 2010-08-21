// 
// ABRecord.cs: Implements the managed ABRecord
//
// Authors: Mono Team
//     
// Copyright (C) 2009 Novell, Inc
//
using System;
using System.Runtime.InteropServices;

using MonoMac.CoreFoundation;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;

namespace MonoMac.AddressBook {

	public enum ABRecordType : uint {
		Person = 0,
		Group = 1,
	}

	public enum ABPropertyType : ushort {
		Invalid         = 0,
		String          = 0x1,
		Integer         = 0x2,
		Real            = 0x3,
		DateTime        = 0x4,
		Dictionary      = 0x5,
		MultiString     = ABMultiValue.Mask | String,
		MultiInteger    = ABMultiValue.Mask | Integer,
		MultiReal       = ABMultiValue.Mask | Real,
		MultiDateTime   = ABMultiValue.Mask | DateTime,
		MultiDictionary = ABMultiValue.Mask | Dictionary,
	}

	public abstract class ABRecord : INativeObject, IDisposable {

		public const int InvalidRecordId = -1;
		public const int InvalidPropertyId = -1;

		IntPtr handle;

		internal ABRecord (IntPtr handle)
		{
			this.handle = handle;
		}

		internal static ABRecord FromHandle (IntPtr handle)
		{
			if (handle == IntPtr.Zero)
				throw new ArgumentNullException ("handle");
			// TODO: does ABGroupCopyArrayOfAllMembers() have Create or Get
			// semantics for the array elements?
			var type = ABRecordGetRecordType (handle);
			switch (type) {
				case ABRecordType.Person:
					return new ABPerson (handle);
				case ABRecordType.Group:
					return new ABGroup (handle);
				default:
					throw new NotSupportedException ("Could not determine record type.");
			}
		}

		~ABRecord ()
		{
			Dispose (false);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (handle != IntPtr.Zero)
				CFObject.CFRelease (handle);
			handle = IntPtr.Zero;
		}

		void AssertValid ()
		{
			if (handle == IntPtr.Zero)
				throw new ObjectDisposedException ("");
		}

		public IntPtr Handle {
			get {
				AssertValid ();
				return handle;
			}
		}

		[DllImport (Constants.AddressBookLibrary)]
		extern static int ABRecordGetRecordID (IntPtr record);
		public int Id {
			get {return ABRecordGetRecordID (Handle);}
		}

		[DllImport (Constants.AddressBookLibrary)]
		extern static ABRecordType ABRecordGetRecordType (IntPtr record);
		public ABRecordType Type {
			get {return ABRecordGetRecordType (Handle);}
		}

		[DllImport (Constants.AddressBookLibrary)]
		extern static IntPtr ABRecordCopyCompositeName (IntPtr record);
		public override string ToString ()
		{
			using (var s = new NSString (ABRecordCopyCompositeName (Handle)))
				return s.ToString ();
		}

		// TODO: Should SetValue/CopyValue/RemoveValue be public?

		[DllImport (Constants.AddressBookLibrary)]
		extern static bool ABRecordSetValue (IntPtr record, int property, IntPtr value, out IntPtr error);
		internal void SetValue (int property, IntPtr value)
		{
			IntPtr error;
			if (!ABRecordSetValue (Handle, property, value, out error))
				throw CFException.FromCFError (error);
		}

		internal void SetValue (int property, NSObject value)
		{
			SetValue (property, value == null ? IntPtr.Zero : value.Handle);
		}

		internal void SetValue (int property, string value)
		{
			using (NSObject v = value == null ? null : new NSString (value))
				SetValue (property, v);
		}

		[DllImport (Constants.AddressBookLibrary)]
		extern static IntPtr ABRecordCopyValue (IntPtr record, int property);
		internal IntPtr CopyValue (int property)
		{
			return ABRecordCopyValue (Handle, property);
		}

		[DllImport (Constants.AddressBookLibrary)]
		extern static bool ABRecordRemoveValue (IntPtr record, int property, out IntPtr error);
		internal void RemoveValue (int property)
		{
			IntPtr error;
			bool r = ABRecordRemoveValue (Handle, property, out error);
			if (!r && error != IntPtr.Zero)
				throw CFException.FromCFError (error);
		}

		internal T PropertyTo<T> (int id)
			where T : NSObject
		{
			IntPtr value = CopyValue (id);
			if (value == IntPtr.Zero)
				return null;
			return (T) Runtime.GetNSObject (value);
		}

		internal string PropertyToString (int id)
		{
			IntPtr value = CopyValue (id);
			if (value == IntPtr.Zero)
				return null;
			using (var s = new NSString (value))
				return s.ToString ();
		}
	}
}
