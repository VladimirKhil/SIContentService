import WellKnownSIContentServiceErrorCode from "./WellKnownSIContentServiceErrorCode";

/** Defines a SIContentService error. */
export default class SIContentServiceError extends Error {
    /** Error code. */
    errorCode?: WellKnownSIContentServiceErrorCode;

    /** Error status code. */
    statusCode: number;

    constructor(message: string | undefined, statusCode: number, errorCode?: WellKnownSIContentServiceErrorCode) {
        super(message);
        this.statusCode = statusCode;
        this.errorCode = errorCode;
    }
}
