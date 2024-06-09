import WellKnownSIContentServiceErrorCode from "./WellKnownSIContentServiceErrorCode";

/** Defines an internal error. */
export default interface InternalError {
    /** Error code. */
    errorCode: WellKnownSIContentServiceErrorCode;
}