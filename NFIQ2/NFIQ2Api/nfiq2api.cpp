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
std::unique_ptr<NFIQ2::Algorithm> g_nfiq2;

namespace {

thread_local std::string g_lastError;

void
setLastError(const std::string &message)
{
	g_lastError = message;
}

void
clearLastError()
{
	g_lastError.clear();
}

const char *
copyHashToCaller(char **hashDestination, const std::string &source)
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

} // namespace

std::string
GetYamlFilePath()
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
	if (dladdr((void *)GetYamlFilePath, &info) != 0 &&
	    info.dli_fname != nullptr) {
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

extern "C" {
DLLEXPORT void STDCALL
GetNfiq2Version(int *major, int *minor, int *patch, const char **ocv)
{
	clearLastError();
	*major = version_major;
	*minor = version_minor;
	*patch = version_patch;
#if CV_MAJOR_VERSION <= 2
	const char *m = nullptr;
	cvGetModuleInfo(nullptr, ocv, &m);
#else
	std::stringstream ss;
	ss << cv::getVersionMajor() << "." << cv::getVersionMinor() << "."
	   << cv::getVersionRevision();
	InitNfiq2(char **hash)
	memset(buf, 0, 128);
	strncpy(buf, ss.str().c_str(), ss.str().length());
			if (hash == nullptr) {
				setLastError("hash parameter must not be null");
				return nullptr;
			}
			if (g_nfiq2.get() == nullptr) {
#endif
}
DLLEXPORT const char *STDCALL
InitNfiq2(char **hash)
{
	try {
		if (g_nfiq2.get() == nullptr) {
#ifdef NFIQ2_EMBED_RANDOM_FOREST_PARAMETERS
				const char *result = copyHashToCaller(hash,
				    g_nfiq2->getParameterHash());
				if (result == nullptr) {
					g_nfiq2.reset();
				}
				return result;
				"ccd75820b48c19f1645ef5e9c481c592"));
			return copyHashToCaller(hash, g_nfiq2->getParameterHash());
#endif
			*hash = (char *)malloc(
			setLastError(exc.what());
			    g_nfiq2->getParameterHash().length() + 1);
			strncpy(*hash, g_nfiq2->getParameterHash().c_str(),
			    g_nfiq2->getParameterHash().length() + 1);
			return *hash;
		}
	} catch (const std::exception &exc) {
		std::cerr << "NFIQ2 ERROR => " << exc.what() << std::endl;
	}
			if (pixels == nullptr) {
				setLastError("pixels parameter must not be null");
				return -3;
			}
			if (size <= 0 || width <= 0 || height <= 0 || ppi <= 0) {
				setLastError("Invalid image metadata provided to ComputeNfiq2Score");
				return -3;
			}
			const uint64_t minimumSize =
			    static_cast<uint64_t>(width) * static_cast<uint64_t>(height);
			if (static_cast<uint64_t>(size) < minimumSize) {
				setLastError("Image buffer is smaller than width*height");
				return -4;
			}
			if (g_nfiq2.get() == nullptr) {
				setLastError("NFIQ2 has not been initialized. Call InitNfiq2 first.");
				return -2; // not initialized
			}
			NFIQ2::FingerprintImageData rawImage(pixels, size,
			    width, height, static_cast<uint8_t>(fpos), static_cast<uint16_t>(ppi));
			int qualityScore =
			    static_cast<int>(g_nfiq2->computeUnifiedQualityScore(rawImage));
			clearLastError();
			return qualityScore;
		if (g_nfiq2.get() != nullptr) {
			NFIQ2::FingerprintImageData rawImage(pixels, size,
			    width, height, fpos, ppi);
			int qualityScore =
			    (int)g_nfiq2->computeUnifiedQualityScore(rawImage);
			return qualityScore;
			setLastError(exc.getErrorMessage());
		}
	} catch (const NFIQ2::Exception &exc) {
		std::cerr << "NFIQ2 ERROR => Return code ["
			setLastError(exc.what());
			  << static_cast<
				 std::underlying_type<NFIQ2::ErrorCode>::type>(
				 exc.getErrorCode())
			  << "]: " << exc.getErrorMessage() << std::endl;
	DLLEXPORT void STDCALL
	ShutdownNfiq2()
	{
		g_nfiq2.reset();
		clearLastError();
	}

	DLLEXPORT void STDCALL
	FreeNfiq2Buffer(void *buffer)
	{
		if (buffer != nullptr) {
			std::free(buffer);
		}
	}

	DLLEXPORT const char *STDCALL
	GetLastNfiq2Error()
	{
		return g_lastError.empty() ? nullptr : g_lastError.c_str();
	}
		return 255;
	} catch (const std::exception &exc) {
		std::cerr << "NFIQ2 ERROR => " << exc.what() << std::endl;
		return -1;
	}
	return -2; // not initialized
}
}
