using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Skopik
{
    public enum SkopikDataType : int
    {
        // not a skopik data type
        // does not necessarily mean it's invalid
        None,
        
        /*
            Object types
        */
        
        Null            = (1 << 0),

        Scope           = (1 << 1),
        Array           = (1 << 2),
        Tuple           = (1 << 3),

        String          = (1 << 4),

        /*
            Special types
        */

        Keyword         = (1 << 5),
        Operator        = (1 << 6),
        Reserved        = (1 << 7),

        /*
            Number types
        */

        Boolean         = (1 << 8),

        Integer         = (1 << 9),

        Float           = (1 << 10),
        Double          = (1 << 11),

        /*
            Number flags
        */

        Signed          = (1 << 12),
        Unsigned        = (1 << 13),

        Long            = (1 << 14),

        BitField        = (1 << 15),
        
        NumberFlagMask  = (Signed | Unsigned | BitField | Long),
        
        /*
            Composite number types
        */

        Binary          = Integer | BitField,

        Integer32       = Integer | Signed,
        Integer64       = Integer | Signed | Long,

        UInteger32      = Integer | Unsigned,
        UInteger64      = Integer | Unsigned | Long,

        /*
            Composite operator types
        */

        OpStmtAssignmt  = (1 << 16) | Operator,
        OpStmtBlock     = (1 << 17) | Operator,

        OpBlockDelim    = (1 << 18) | Operator,
        OpBlockOpen     = (1 << 19) | Operator,
        OpBlockClose    = (1 << 20) | Operator,

        OpScopeDelim    = Scope | OpBlockDelim,
        OpScopeOpen     = Scope | OpBlockOpen,
        OpScopeClose    = Scope | OpBlockClose,

        OpArrayDelim    = Array | OpBlockDelim,
        OpArrayOpen     = Array | OpBlockOpen,
        OpArrayClose    = Array | OpBlockClose,
        
        OpTupleDelim    = Tuple | OpBlockDelim,
        OpTupleOpen     = Tuple | OpBlockOpen,
        OpTupleClose    = Tuple | OpBlockClose,
    }
}
