using System;
using System.Runtime.InteropServices;

// Contains IFilter interface translation
// Most translations are from PInvoke.net
namespace EPocalipse.IFilter
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Fullpropspec
	{
		#region Fields

		public Guid guidPropSet;
		public Propspec psProperty;

		#endregion
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct Filterregion
	{
		#region Fields

		public int cwcExtent;
		public int cwcStart;
		public int idChunk;

		#endregion
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct Propspec
	{
		#region Fields

		[FieldOffset(4)] public IntPtr lpwstr;
		[FieldOffset(4)] public int propid;
		[FieldOffset(0)] public int ulKind; // 0 - string used; 1 - PROPID

		#endregion
	}

	[Flags]
	internal enum IfilterFlags
	{
		/// <summary>
		/// 	The caller should use the IPropertySetStorage and IPropertyStorage
		/// 	interfaces to locate additional properties. 
		/// 	When this flag is set, properties available through COM
		/// 	enumerators should not be returned from IFilter.
		/// </summary>
		IfilterFlagsOleProperties = 1
	}

	/// <summary>
	/// 	Flags controlling the operation of the FileFilter
	/// 	instance.
	/// </summary>
	[Flags]
	internal enum IfilterInit
	{
		None = 0,
		/// <summary>
		/// 	Paragraph breaks should be marked with the Unicode PARAGRAPH
		/// 	SEPARATOR (0x2029)
		/// </summary>
		CanonParagraphs = 1,

		/// <summary>
		/// 	Soft returns, such as the newline character in Microsoft Word, should
		/// 	be replaced by hard returnsLINE SEPARATOR (0x2028). Existing hard
		/// 	returns can be doubled. A carriage return (0x000D), line feed (0x000A),
		/// 	or the carriage return and line feed in combination should be considered
		/// 	a hard return. The intent is to enable pattern-expression matches that
		/// 	match against observed line breaks.
		/// </summary>
		HardLineBreaks = 2,

		/// <summary>
		/// 	Various word-processing programs have forms of hyphens that are not
		/// 	represented in the host character set, such as optional hyphens
		/// 	(appearing only at the end of a line) and nonbreaking hyphens. This flag
		/// 	indicates that optional hyphens are to be converted to nulls, and
		/// 	non-breaking hyphens are to be converted to normal hyphens (0x2010), or
		/// 	HYPHEN-MINUSES (0x002D).
		/// </summary>
		CanonHyphens = 4,

		/// <summary>
		/// 	Just as the CANON_HYPHENS flag standardizes hyphens,
		/// 	this one standardizes spaces. All special space characters, such as
		/// 	nonbreaking spaces, are converted to the standard space character
		/// 	(0x0020).
		/// </summary>
		CanonSpaces = 8,

		/// <summary>
		/// 	Indicates that the client wants text split into chunks representing
		/// 	public value-type properties.
		/// </summary>
		ApplyIndexAttributes = 16,

		/// <summary>
		/// 	Indicates that the client wants text split into chunks representing
		/// 	properties determined during the indexing process.
		/// </summary>
		ApplyCrawlAttributes = 256,

		/// <summary>
		/// 	Any properties not covered by the APPLY_INDEX_ATTRIBUTES
		/// 	and APPLY_CRAWL_ATTRIBUTES flags should be emitted.
		/// </summary>
		ApplyOtherAttributes = 32,

		/// <summary>
		/// 	Optimizes IFilter for indexing because the client calls the
		/// 	IFilter::Init method only once and does not call IFilter::BindRegion.
		/// 	This eliminates the possibility of accessing a chunk both before and
		/// 	after accessing another chunk.
		/// </summary>
		IndexingOnly = 64,

		/// <summary>
		/// 	The text extraction process must recursively search all linked
		/// 	objects within the document. If a link is unavailable, the
		/// 	IFilter::GetChunk call that would have obtained the first chunk of the
		/// 	link should return FILTER_E_LINK_UNAVAILABLE.
		/// </summary>
		SearchLinks = 128,

		/// <summary>
		/// 	The content indexing process can return property values set by the  filter.
		/// </summary>
		FilterOwnedValueOk = 512
	}

	public struct StatChunk
	{
		#region Fields

		/// <summary>
		/// 	The property to be applied to the chunk. If a filter requires that       the same text 
		/// 	have more than one property, it needs to emit the text once for each       property 
		/// 	in separate chunks.
		/// </summary>
		public Fullpropspec Attribute;

		/// <summary>
		/// 	The type of break that separates the previous chunk from the current
		/// 	chunk. Values are from the CHUNK_BREAKTYPE enumeration.
		/// </summary>
		[MarshalAs(UnmanagedType.U4)] public ChunkBreaktype BreakType;

		/// <summary>
		/// 	The length in characters of the source text from which the current
		/// 	chunk was derived. 
		/// 	A zero value signifies character-by-character correspondence between
		/// 	the source text and 
		/// 	the derived text. A nonzero value means that no such direct
		/// 	correspondence exists
		/// </summary>
		public int CwcLenSource;

		/// <summary>
		/// 	The offset from which the source text for a derived chunk starts in
		/// 	the source chunk.
		/// </summary>
		public int CwcStartSource;

		/// <summary>
		/// 	Flags indicate whether this chunk contains a text-type or a
		/// 	value-type property. 
		/// 	Flag values are taken from the CHUNKSTATE enumeration. If the CHUNK_TEXT flag is set, 
		/// 	IFilter::GetText should be used to retrieve the contents of the chunk
		/// 	as a series of words. 
		/// 	If the CHUNK_VALUE flag is set, IFilter::GetValue should be used to retrieve 
		/// 	the value and treat it as a single property value. If the filter dictates that the same 
		/// 	content be treated as both text and as a value, the chunk should be emitted twice in two       
		/// 	different chunks, each with one flag set.
		/// </summary>
		[MarshalAs(UnmanagedType.U4)] public Chunkstate Flags;

		/// <summary>
		/// 	The chunk identifier. Chunk identifiers must be unique for the
		/// 	current instance of the IFilter interface. 
		/// 	Chunk identifiers must be in ascending order. The order in which
		/// 	chunks are numbered should correspond to the order in which they appear
		/// 	in the source document. Some search engines can take advantage of the
		/// 	proximity of chunks of various properties. If so, the order in which
		/// 	chunks with different properties are emitted will be important to the
		/// 	search engine.
		/// </summary>
		public int IDChunk;

		/// <summary>
		/// 	The ID of the source of a chunk. The value of the idChunkSource     member depends on the nature of the chunk: 
		/// 	If the chunk is a text-type property, the value of the idChunkSource       member must be the same as the value of the idChunk member. 
		/// 	If the chunk is an public value-type property derived from textual       content, the value of the idChunkSource member is the chunk ID for the
		/// 	text-type chunk from which it is derived. 
		/// 	If the filter attributes specify to return only public value-type
		/// 	properties, there is no content chunk from which to derive the current
		/// 	public value-type property. In this case, the value of the
		/// 	idChunkSource member must be set to zero, which is an invalid chunk.
		/// </summary>
		public int IDChunkSource;

		/// <summary>
		/// 	The language and sublanguage associated with a chunk of text. Chunk locale is used 
		/// 	by document indexers to perform proper word breaking of text. If the chunk is 
		/// 	neither text-type nor a value-type with data type VT_LPWSTR, VT_LPSTR or VT_BSTR, 
		/// 	this field is ignored.
		/// </summary>
		public int Locale;

		#endregion
	}

	/// <summary>
	/// 	Enumerates the different breaking types that occur between 
	/// 	chunks of text read out by the FileFilter.
	/// </summary>
	public enum ChunkBreaktype
	{
		/// <summary>
		/// 	No break is placed between the current chunk and the previous chunk.
		/// 	The chunks are glued together.
		/// </summary>
		ChunkNoBreak = 0,
		/// <summary>
		/// 	A word break is placed between this chunk and the previous chunk that
		/// 	had the same attribute. 
		/// 	Use of CHUNK_EOW should be minimized because the choice of word
		/// 	breaks is language-dependent, 
		/// 	so determining word breaks is best left to the search engine.
		/// </summary>
		ChunkEow = 1,
		/// <summary>
		/// 	A sentence break is placed between this chunk and the previous chunk
		/// 	that had the same attribute.
		/// </summary>
		ChunkEos = 2,
		/// <summary>
		/// 	A paragraph break is placed between this chunk and the previous chunk
		/// 	that had the same attribute.
		/// </summary>
		ChunkEop = 3,
		/// <summary>
		/// 	A chapter break is placed between this chunk and the previous chunk
		/// 	that had the same attribute.
		/// </summary>
		ChunkEoc = 4
	}

	[Flags]
	public enum Chunkstate
	{
		/// <summary>
		/// 	The current chunk is a text-type property.
		/// </summary>
		ChunkText = 0x1,
		/// <summary>
		/// 	The current chunk is a value-type property.
		/// </summary>
		ChunkValue = 0x2,
		/// <summary>
		/// 	Reserved
		/// </summary>
		ChunkFilterOwnedValue = 0x4
	}

	internal enum FilterReturnCode : uint
	{
		/// <summary>
		/// 	Success
		/// </summary>
		SOk = 0,
		/// <summary>
		/// 	The function was denied access to the filter file.
		/// </summary>
		EAccessdenied = 0x80070005,
		/// <summary>
		/// 	The function encountered an invalid handle,
		/// 	probably due to a low-memory situation.
		/// </summary>
		EHandle = 0x80070006,
		/// <summary>
		/// 	The function received an invalid parameter.
		/// </summary>
		EInvalidarg = 0x80070057,
		/// <summary>
		/// 	Out of memory
		/// </summary>
		EOutofmemory = 0x8007000E,
		/// <summary>
		/// 	Not implemented
		/// </summary>
		ENotimpl = 0x80004001,
		/// <summary>
		/// 	Unknown error
		/// </summary>
		EFail = 0x80000008,
		/// <summary>
		/// 	File not filtered due to password protection
		/// </summary>
		FilterEPassword = 0x8004170B,
		/// <summary>
		/// 	The document format is not recognised by the filter
		/// </summary>
		FilterEUnknownformat = 0x8004170C,
		/// <summary>
		/// 	No text in current chunk
		/// </summary>
		FilterENoText = 0x80041705,
		/// <summary>
		/// 	No more chunks of text available in object
		/// </summary>
		FilterEEndOfChunks = 0x80041700,
		/// <summary>
		/// 	No more text available in chunk
		/// </summary>
		FilterENoMoreText = 0x80041701,
		/// <summary>
		/// 	No more property values available in chunk
		/// </summary>
		FilterENoMoreValues = 0x80041702,
		/// <summary>
		/// 	Unable to access object
		/// </summary>
		FilterEAccess = 0x80041703,
		/// <summary>
		/// 	Moniker doesn't cover entire region
		/// </summary>
		FilterWMonikerClipped = 0x00041704,
		/// <summary>
		/// 	Unable to bind IFilter for embedded object
		/// </summary>
		FilterEEmbeddingUnavailable = 0x80041707,
		/// <summary>
		/// 	Unable to bind IFilter for linked object
		/// </summary>
		FilterELinkUnavailable = 0x80041708,
		/// <summary>
		/// 	This is the last text in the current chunk
		/// </summary>
		FilterSLastText = 0x00041709,
		/// <summary>
		/// 	This is the last value in the current chunk
		/// </summary>
		FilterSLastValues = 0x0004170A
	}

	[ComImport, Guid("89BCB740-6119-101A-BCB7-00DD010655AF")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IFilter
	{
		/// <summary>
		/// 	The IFilter::Init method initializes a filtering session.
		/// </summary>
		[PreserveSig]
		FilterReturnCode Init(
			//[in] Flag settings from the IFILTER_INIT enumeration for
			// controlling text standardization, property output, embedding
			// scope, and IFilter access patterns. 
			IfilterInit grfFlags,
			// [in] The size of the attributes array. When nonzero, cAttributes
			//  takes 
			// precedence over attributes specified in grfFlags. If no
			// attribute flags 
			// are specified and cAttributes is zero, the default is given by
			// the 
			// PSGUID_STORAGE storage property set, which contains the date and
			//  time 
			// of the last write to the file, size, and so on; and by the
			//  PID_STG_CONTENTS 
			// 'contents' property, which maps to the main contents of the
			// file. 
			// For more information about properties and property sets, see
			// Property Sets. 
			int cAttributes,
			//[in] Array of pointers to FULLPROPSPEC structures for the
			// requested properties. 
			// When cAttributes is nonzero, only the properties in aAttributes
			// are returned. 
			IntPtr aAttributes,
			// [out] Information about additional properties available to the
			//  caller; from the IFILTER_FLAGS enumeration. 
			out IfilterFlags pdwFlags);

		/// <summary>
		/// 	The IFilter::GetChunk method positions the filter at the beginning
		/// 	of the next chunk, 
		/// 	or at the first chunk if this is the first call to the GetChunk
		/// 	method, and returns a description of the current chunk.
		/// </summary>
		[PreserveSig]
		FilterReturnCode GetChunk(out StatChunk pStat);

		/// <summary>
		/// 	The IFilter::GetText method retrieves text (text-type properties)
		/// 	from the current chunk, 
		/// 	which must have a CHUNKSTATE enumeration value of CHUNK_TEXT.
		/// </summary>
		[PreserveSig]
		FilterReturnCode GetText(
			// [in/out] On entry, the size of awcBuffer array in wide/Unicode
			// characters. On exit, the number of Unicode characters written to
			// awcBuffer. 
			// Note that this value is not the number of bytes in the buffer. 
			ref uint pcwcBuffer,
			// Text retrieved from the current chunk. Do not terminate the
			// buffer with a character.  
			[Out, MarshalAs(UnmanagedType.LPArray)] char[] awcBuffer);

		/// <summary>
		/// 	The IFilter::GetValue method retrieves a value (public
		/// 	value-type property) from a chunk, 
		/// 	which must have a CHUNKSTATE enumeration value of CHUNK_VALUE.
		/// </summary>
		[PreserveSig]
		int GetValue(
			// Allocate the PROPVARIANT structure with CoTaskMemAlloc. Some
			// PROPVARIANT 
			// structures contain pointers, which can be freed by calling the
			// PropVariantClear function. 
			// It is up to the caller of the GetValue method to call the
			//   PropVariantClear method.            
			// ref IntPtr ppPropValue
			// [MarshalAs(UnmanagedType.Struct)]
			ref IntPtr propVal);

		/// <summary>
		/// 	The IFilter::BindRegion method retrieves an interface representing
		/// 	the specified portion of the object. 
		/// 	Currently reserved for future use.
		/// </summary>
		[PreserveSig]
		int BindRegion(ref Filterregion origPos,
			ref Guid riid, ref object ppunk);
	}
}