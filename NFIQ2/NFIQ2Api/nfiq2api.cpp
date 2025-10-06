#include <nfiq2_algorithm.hpp>
#include <nfiq2_exception.hpp>
#include <opencv2/core/core_c.h>
#include <opencv2/core/version.hpp>

#include "nfiq2api.h"

#include <cstdlib>
#include <cstring>
#include <iostream>
#include <memory>
#include <sstream>
#include <string>
#include <cstdint>
#include <cstdio>

#ifndef _WIN32
#include <dlfcn.h>
#endif

// external vars for the version values
extern char product_name[128];
extern char product_vendor[128];
extern int version_major;
extern int version_minor;
extern int version_patch;

// static object to load the algorithm only once (random forest init!)
static std::unique_ptr<NFIQ2::Algorithm> g_nfiq2;

namespace {

thread_local std::string g_lastError;

void setLastError(const std::string &message) { g_lastError = message; }
void clearLastError() { g_lastError.clear(); }

const char *copyHashToCaller(char **hashDestination, const std::string &source)
{
	if (hashDestination == nullptr) {
		setLastError("hash parameter must not be null");
		return nullptr;
	}
	*hashDestination = static_cast<char *>(std::malloc(source.length() + 1));
	if (*hashDestination == nullptr) {
		setLastError("Failed to allocate memory for parameter hash");
		return nullptr;
	}
	std::memcpy(*hashDestination, source.c_str(), source.length() + 1);
	clearLastError();
	return *hashDestination;
}

std::string GetYamlFilePath()
{
	std::string p;
#ifdef _WIN32
	HMODULE hmodule = NULL;
	GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS |
		GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
	    reinterpret_cast<LPCSTR>(GetYamlFilePath), &hmodule);
	char buffer[MAX_PATH];
	int n = GetModuleFileNameA(hmodule, buffer, sizeof(buffer));
	if (n > 0 && n < (int)sizeof(buffer)) {
		char *c = strrchr(buffer, '\\');
		if (c != nullptr) {
			*c = 0;
			p = buffer;
			p += "\\nist_plain_tir-ink.yaml";
		}
	}
#else
	Dl_info info;
	if (dladdr((void *)GetYamlFilePath, &info) != 0 && info.dli_fname != nullptr) {
		char *c = (char *)strrchr(info.dli_fname, '/');
		if (c != nullptr) {
			*c = 0;
			p = info.dli_fname;
			p += "/nist_plain_tir-ink.yaml";
		}
	}
#endif
	return p;
}

} // namespace

extern "C" {

DLLEXPORT void STDCALL GetNfiq2Version(int *major, int *minor, int *patch, const char **ocv)
{
	clearLastError();
	if (major) *major = version_major; else setLastError("major pointer null");
	if (minor) *minor = version_minor; else setLastError("minor pointer null");
	if (patch) *patch = version_patch; else setLastError("patch pointer null");
#if CV_MAJOR_VERSION <= 2
	if (ocv) {
		const char *m = nullptr;
		cvGetModuleInfo(nullptr, ocv, &m);
	}
#else
	if (ocv) {
		static char buf[64];
		std::snprintf(buf, sizeof(buf), "%d.%d.%d", cv::getVersionMajor(), cv::getVersionMinor(), cv::getVersionRevision());
		*ocv = buf;
	}
#endif
}

DLLEXPORT const char *STDCALL InitNfiq2(char **hash)
{
	try {
		if (!g_nfiq2) {
#ifdef NFIQ2_EMBED_RANDOM_FOREST_PARAMETERS
			g_nfiq2.reset(new NFIQ2::Algorithm());
#else
			// Known model hash from distributed YAML (seen in original source fragment)
			static const std::string kModelHash = "ccd75820b48c19f1645ef5e9c481c592";
			g_nfiq2.reset(new NFIQ2::Algorithm(GetYamlFilePath(), kModelHash));
#endif
			if (!g_nfiq2 || !g_nfiq2->isInitialized()) {
				setLastError("Failed to initialize NFIQ2 algorithm");
				g_nfiq2.reset();
				return nullptr;
			}
		}
		return copyHashToCaller(hash, g_nfiq2->getParameterHash());
	} catch (const NFIQ2::Exception &exc) {
		setLastError(exc.getErrorMessage());
		std::cerr << "NFIQ2 ERROR => " << exc.getErrorMessage() << std::endl;
		g_nfiq2.reset();
		return nullptr;
	} catch (const std::exception &exc) {
		setLastError(exc.what());
		std::cerr << "NFIQ2 ERROR => " << exc.what() << std::endl;
		g_nfiq2.reset();
		return nullptr;
	}
}

DLLEXPORT int STDCALL ComputeNfiq2Score(int fpos, const unsigned char *pixels,
    int size, int width, int height, int ppi)
{
	try {
		if (pixels == nullptr) {
			setLastError("pixels parameter must not be null");
			return -3;
		}
		if (size <= 0 || width <= 0 || height <= 0 || ppi <= 0) {
			setLastError("Invalid image metadata provided to ComputeNfiq2Score");
			return -3;
		}
		const uint64_t minimumSize = static_cast<uint64_t>(width) * static_cast<uint64_t>(height);
		if (static_cast<uint64_t>(size) < minimumSize) {
			setLastError("Image buffer is smaller than width*height");
			return -4;
		}
		if (!g_nfiq2) {
			setLastError("NFIQ2 has not been initialized. Call InitNfiq2 first.");
			return -2; // not initialized
		}
		NFIQ2::FingerprintImageData rawImage(pixels, size, width, height,
		    static_cast<uint8_t>(fpos), static_cast<uint16_t>(ppi));
		int qualityScore = static_cast<int>(g_nfiq2->computeUnifiedQualityScore(rawImage));
		clearLastError();
		return qualityScore;
	} catch (const NFIQ2::Exception &exc) {
		setLastError(exc.getErrorMessage());
		std::cerr << "NFIQ2 ERROR => Return code [" << static_cast<std::underlying_type<NFIQ2::ErrorCode>::type>(exc.getErrorCode())
		          << "]: " << exc.getErrorMessage() << std::endl;
		return 255; // mimic legacy sentinel, caller can inspect GetLastNfiq2Error
	} catch (const std::exception &exc) {
		setLastError(exc.what());
		std::cerr << "NFIQ2 ERROR => " << exc.what() << std::endl;
		return -1;
	}
}

DLLEXPORT void STDCALL ShutdownNfiq2()
{
	g_nfiq2.reset();
	clearLastError();
}

DLLEXPORT void STDCALL FreeNfiq2Buffer(void *buffer)
{
	if (buffer != nullptr) {
		std::free(buffer);
	}
}

DLLEXPORT const char *STDCALL GetLastNfiq2Error()
{
	return g_lastError.empty() ? nullptr : g_lastError.c_str();
}

} // extern "C"
