/* Auto-generated table of all pre-included modules, rebuilt
 * by Configure script.  DO NOT EDIT BY HAND
 */

#include "conf.h"

extern module core_module;
extern module xfer_module;
extern module auth_unix_module;
extern module auth_file_module;
extern module auth_module;
extern module ls_module;
extern module log_module;
extern module site_module;
extern module delay_module;
extern module auth_pam_module;
extern module cap_module;
extern module ctrls_module;

module *static_modules[] = {
  &core_module,
  &xfer_module,
  &auth_unix_module,
  &auth_file_module,
  &auth_module,
  &ls_module,
  &log_module,
  &site_module,
  &delay_module,
  &auth_pam_module,
  &cap_module,
  &ctrls_module,
  NULL
};

module *loaded_modules = NULL;
